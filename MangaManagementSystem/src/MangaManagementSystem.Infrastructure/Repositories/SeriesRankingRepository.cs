using MangaManagementSystem.Application.Features.Ranking.Dtos;
using MangaManagementSystem.Application.Features.Ranking.Repositories;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories;

public sealed class SeriesRankingRepository : ISeriesRankingRepository
{
    private readonly ApplicationDbContext _context;

    public SeriesRankingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PublicationPeriodDto>> GetWeeklyPublicationPeriodsAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Set<PublicationPeriod>()
            .AsNoTracking()
            .Where(period => period.PeriodTypeCode == "WEEKLY")
            .OrderByDescending(period => period.PeriodStartDate)
            .Select(period => new PublicationPeriodDto(
                period.PublicationPeriodId,
                period.PeriodName,
                period.PeriodTypeCode,
                period.PeriodStartDate,
                period.PeriodEndDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeriesVoteInputDto>> GetSeriesVoteInputsAsync(
        Guid publicationPeriodId,
        string? searchText,
        string? sort,
        CancellationToken cancellationToken)
    {
        IQueryable<SeriesVoteInputDto> query =
            from input in _context.Set<SeriesVoteInput>().AsNoTracking()
            join series in _context.Set<Series>().AsNoTracking()
                on input.SeriesId equals series.SeriesId
            join cover in _context.Set<FileResource>().AsNoTracking()
                    .Where(file => file.DeletedAtUtc == null)
                on series.CoverFileId equals cover.FileResourceId into coverGroup
            from cover in coverGroup.DefaultIfEmpty()
            where input.PublicationPeriodId == publicationPeriodId
            select new SeriesVoteInputDto(
                input.SeriesVoteInputId,
                input.PublicationPeriodId,
                input.SeriesId,
                series.Title,
                series.Slug,
                cover == null ? null : cover.CloudinarySecureUrl,
                input.RatingCount,
                input.AverageRating,
                input.ReadingCount,
                input.DataSourceNote,
                input.EnteredByUserId,
                input.EnteredAtUtc,
                input.UpdatedByUserId,
                input.UpdatedAtUtc);

        var normalizedSearch = NormalizeSearch(searchText);

        if (normalizedSearch is not null)
        {
            query = query.Where(row => row.SeriesTitle.Contains(normalizedSearch));
        }

        query = ApplyVoteInputSort(query, sort);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeriesRankingRowDto>> GetSeriesRankingAsync(
        Guid publicationPeriodId,
        string? searchText,
        string? sort,
        CancellationToken cancellationToken)
    {
        IQueryable<SeriesRankingRowDto> query =
            from ranking in _context.Set<SeriesRankingViewRow>().AsNoTracking()
            join series in _context.Set<Series>().AsNoTracking()
                on ranking.SeriesId equals series.SeriesId
            join cover in _context.Set<FileResource>().AsNoTracking()
                    .Where(file => file.DeletedAtUtc == null)
                on series.CoverFileId equals cover.FileResourceId into coverGroup
            from cover in coverGroup.DefaultIfEmpty()
            where ranking.PublicationPeriodId == publicationPeriodId
            select new SeriesRankingRowDto(
                ranking.PublicationPeriodId,
                ranking.PeriodName,
                ranking.PeriodTypeCode,
                ranking.PeriodStartDate,
                ranking.PeriodEndDate,
                ranking.SeriesId,
                ranking.Title,
                ranking.Slug,
                cover == null ? null : cover.CloudinarySecureUrl,
                ranking.RatingCount,
                ranking.AverageRating,
                ranking.ReadingCount,
                ranking.RankingScore,
                ranking.RankPosition);

        var normalizedSearch = NormalizeSearch(searchText);

        if (normalizedSearch is not null)
        {
            query = query.Where(row => row.SeriesTitle.Contains(normalizedSearch));
        }

        query = ApplyRankingSort(query, sort);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RankableSeriesDto>> SearchRankableSeriesAsync(
        Guid publicationPeriodId,
        string? searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = NormalizeSearch(searchText);
        var safeMaxResults = maxResults <= 0 ? 20 : maxResults;

        /*
            Important:
            This dropdown must show series from the real DB.
            Do NOT restrict only to SERIALIZED / HIATUS / COMPLETED here,
            because your test data may still be PROPOSAL_DRAFT,
            UNDER_EDITORIAL_REVIEW, or UNDER_BOARD_REVIEW.

            Duplicate vote input is still protected in CreateSeriesVoteInputAsync.
        */
        var query =
            from series in _context.Set<Series>().AsNoTracking()
            join cover in _context.Set<FileResource>().AsNoTracking()
                    .Where(file => file.DeletedAtUtc == null)
                on series.CoverFileId equals cover.FileResourceId into coverGroup
            from cover in coverGroup.DefaultIfEmpty()
            where series.StatusCode != "CANCELLED"
            select new RankableSeriesDto(
                series.SeriesId,
                series.Title,
                series.Slug,
                cover == null ? null : cover.CloudinarySecureUrl);

        if (normalizedSearch is not null)
        {
            query = query.Where(row => row.Title.Contains(normalizedSearch));
        }

        return await query
            .OrderBy(row => row.Title)
            .Take(safeMaxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsVoteInputActorAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return await _context.Set<User>()
            .AsNoTracking()
            .Include(user => user.Role)
            .AnyAsync(
                user => user.UserId == actorUserId
                        && user.StatusCode == "ACTIVE"
                        && user.Role != null
                        && (user.Role.RoleName == "Editorial Board Member"
                            || user.Role.RoleName == "EditorialBoardMember"
                            || user.Role.RoleName == "Board Member"
                            || user.Role.RoleName == "Editorial Board Chief"
                            || user.Role.RoleName == "EditorialBoardChief"
                            || user.Role.RoleName == "Board Chief"),
                cancellationToken);
    }

    public async Task<SeriesVoteInputDto> CreateSeriesVoteInputAsync(
        Guid actorUserId,
        Guid publicationPeriodId,
        Guid seriesId,
        int ratingCount,
        decimal averageRating,
        int readingCount,
        string? dataSourceNote,
        CancellationToken cancellationToken)
    {
        var periodExists = await _context.Set<PublicationPeriod>()
            .AnyAsync(
                period => period.PublicationPeriodId == publicationPeriodId,
                cancellationToken);

        if (!periodExists)
        {
            throw new InvalidOperationException("Publication period was not found.");
        }

        var seriesExists = await _context.Set<Series>()
            .AnyAsync(
                series => series.SeriesId == seriesId
                          && series.StatusCode != "CANCELLED",
                cancellationToken);

        if (!seriesExists)
        {
            throw new InvalidOperationException("Series was not found or cannot be ranked.");
        }

        var duplicateExists = await _context.Set<SeriesVoteInput>()
            .AnyAsync(
                input => input.PublicationPeriodId == publicationPeriodId
                         && input.SeriesId == seriesId,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "This series already has vote input for the selected publication period.");
        }

        var input = new SeriesVoteInput
        {
            PublicationPeriodId = publicationPeriodId,
            SeriesId = seriesId,
            RatingCount = ratingCount,
            AverageRating = averageRating,
            ReadingCount = readingCount,
            DataSourceNote = dataSourceNote,
            EnteredByUserId = actorUserId,
            EnteredAtUtc = DateTime.UtcNow
        };

        _context.Set<SeriesVoteInput>().Add(input);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRequiredVoteInputAsync(
            input.SeriesVoteInputId,
            cancellationToken);
    }

    public async Task<SeriesVoteInputDto> UpdateSeriesVoteInputAsync(
        Guid actorUserId,
        Guid seriesVoteInputId,
        int ratingCount,
        decimal averageRating,
        int readingCount,
        string? dataSourceNote,
        CancellationToken cancellationToken)
    {
        var input = await _context.Set<SeriesVoteInput>()
            .FirstOrDefaultAsync(
                item => item.SeriesVoteInputId == seriesVoteInputId,
                cancellationToken);

        if (input is null)
        {
            throw new InvalidOperationException("Series vote input was not found.");
        }

        input.RatingCount = ratingCount;
        input.AverageRating = averageRating;
        input.ReadingCount = readingCount;
        input.DataSourceNote = dataSourceNote;
        input.UpdatedByUserId = actorUserId;
        input.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRequiredVoteInputAsync(
            seriesVoteInputId,
            cancellationToken);
    }

    private async Task<SeriesVoteInputDto> GetRequiredVoteInputAsync(
        Guid seriesVoteInputId,
        CancellationToken cancellationToken)
    {
        var result = await (
            from input in _context.Set<SeriesVoteInput>().AsNoTracking()
            join series in _context.Set<Series>().AsNoTracking()
                on input.SeriesId equals series.SeriesId
            join cover in _context.Set<FileResource>().AsNoTracking()
                    .Where(file => file.DeletedAtUtc == null)
                on series.CoverFileId equals cover.FileResourceId into coverGroup
            from cover in coverGroup.DefaultIfEmpty()
            where input.SeriesVoteInputId == seriesVoteInputId
            select new SeriesVoteInputDto(
                input.SeriesVoteInputId,
                input.PublicationPeriodId,
                input.SeriesId,
                series.Title,
                series.Slug,
                cover == null ? null : cover.CloudinarySecureUrl,
                input.RatingCount,
                input.AverageRating,
                input.ReadingCount,
                input.DataSourceNote,
                input.EnteredByUserId,
                input.EnteredAtUtc,
                input.UpdatedByUserId,
                input.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return result
            ?? throw new InvalidOperationException(
                "Series vote input could not be loaded after saving.");
    }

    private static string? NormalizeSearch(string? searchText)
    {
        var normalized = searchText?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }

    private static IQueryable<SeriesVoteInputDto> ApplyVoteInputSort(
        IQueryable<SeriesVoteInputDto> query,
        string? sort)
    {
        return sort switch
        {
            "title_desc" => query.OrderByDescending(row => row.SeriesTitle),
            "average_rating_desc" => query.OrderByDescending(row => row.AverageRating),
            "average_rating_asc" => query.OrderBy(row => row.AverageRating),
            "reading_count_desc" => query.OrderByDescending(row => row.ReadingCount),
            "reading_count_asc" => query.OrderBy(row => row.ReadingCount),
            "rating_count_desc" => query.OrderByDescending(row => row.RatingCount),
            "rating_count_asc" => query.OrderBy(row => row.RatingCount),
            "updated_desc" => query.OrderByDescending(row => row.UpdatedAtUtc ?? row.EnteredAtUtc),
            "updated_asc" => query.OrderBy(row => row.UpdatedAtUtc ?? row.EnteredAtUtc),
            _ => query.OrderBy(row => row.SeriesTitle)
        };
    }

    private static IQueryable<SeriesRankingRowDto> ApplyRankingSort(
        IQueryable<SeriesRankingRowDto> query,
        string? sort)
    {
        return sort switch
        {
            "rank_desc" => query.OrderByDescending(row => row.RankPosition),
            "title_asc" => query.OrderBy(row => row.SeriesTitle),
            "title_desc" => query.OrderByDescending(row => row.SeriesTitle),
            "score_desc" => query.OrderByDescending(row => row.RankingScore),
            "score_asc" => query.OrderBy(row => row.RankingScore),
            "average_rating_desc" => query.OrderByDescending(row => row.AverageRating),
            "average_rating_asc" => query.OrderBy(row => row.AverageRating),
            "reading_count_desc" => query.OrderByDescending(row => row.ReadingCount),
            "reading_count_asc" => query.OrderBy(row => row.ReadingCount),
            "rating_count_desc" => query.OrderByDescending(row => row.RatingCount),
            "rating_count_asc" => query.OrderBy(row => row.RatingCount),
            _ => query.OrderBy(row => row.RankPosition)
        };
    }
}