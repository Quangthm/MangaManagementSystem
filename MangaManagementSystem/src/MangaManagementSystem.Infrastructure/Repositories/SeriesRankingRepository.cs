using MangaManagementSystem.Application.Features.Ranking.Dtos;
using MangaManagementSystem.Application.Features.Ranking.Repositories;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories;

public sealed class SeriesRankingRepository : ISeriesRankingRepository
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public SeriesRankingRepository(
        ApplicationDbContext context,
        TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<PublicationPeriodDto>>
        GetWeeklyPublicationPeriodsAsync(
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

    public async Task<IReadOnlyList<SeriesVoteInputDto>>
        GetSeriesVoteInputsAsync(
            Guid publicationPeriodId,
            string? searchText,
            string? sort,
            CancellationToken cancellationToken)
    {
        var query =
            from input in _context.Set<SeriesVoteInput>().AsNoTracking()
            join series in _context.Set<Series>().AsNoTracking()
                on input.SeriesId equals series.SeriesId
            join cover in _context.Set<FileResource>()
                    .AsNoTracking()
                    .Where(file => file.DeletedAtUtc == null)
                on series.CoverFileId equals cover.FileResourceId
                into coverGroup
            from cover in coverGroup.DefaultIfEmpty()
            where input.PublicationPeriodId == publicationPeriodId
            select new
            {
                Input = input,
                Series = series,
                Cover = cover
            };

        var normalizedSearch = NormalizeSearch(searchText);

        if (normalizedSearch is not null)
        {
            query = query.Where(row =>
                row.Series.Title.Contains(normalizedSearch));
        }

        query = sort switch
        {
            "title_desc" =>
                query.OrderByDescending(row => row.Series.Title),

            "average_rating_desc" =>
                query.OrderByDescending(row => row.Input.AverageRating),

            "average_rating_asc" =>
                query.OrderBy(row => row.Input.AverageRating),

            "reading_count_desc" =>
                query.OrderByDescending(row => row.Input.ReadingCount),

            "reading_count_asc" =>
                query.OrderBy(row => row.Input.ReadingCount),

            "rating_count_desc" =>
                query.OrderByDescending(row => row.Input.RatingCount),

            "rating_count_asc" =>
                query.OrderBy(row => row.Input.RatingCount),

            "updated_desc" =>
                query.OrderByDescending(row =>
                    row.Input.UpdatedAtUtc ?? row.Input.EnteredAtUtc),

            "updated_asc" =>
                query.OrderBy(row =>
                    row.Input.UpdatedAtUtc ?? row.Input.EnteredAtUtc),

            _ => query.OrderBy(row => row.Series.Title)
        };

        return await query
            .Select(row => new SeriesVoteInputDto(
                row.Input.SeriesVoteInputId,
                row.Input.PublicationPeriodId,
                row.Input.SeriesId,
                row.Series.Title,
                row.Series.Slug,
                row.Cover == null
                    ? null
                    : row.Cover.CloudinarySecureUrl,
                row.Input.RatingCount,
                row.Input.AverageRating,
                row.Input.ReadingCount,
                row.Input.DataSourceNote,
                row.Input.EnteredByUserId,
                row.Input.EnteredAtUtc,
                row.Input.UpdatedByUserId,
                row.Input.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeriesRankingRowDto>>
        GetSeriesRankingAsync(
            Guid publicationPeriodId,
            string? searchText,
            string? sort,
            CancellationToken cancellationToken)
    {
        var query =
            from ranking in _context.Set<SeriesRankingViewRow>()
                .AsNoTracking()
            join series in _context.Set<Series>().AsNoTracking()
                on ranking.SeriesId equals series.SeriesId
            join cover in _context.Set<FileResource>()
                    .AsNoTracking()
                    .Where(file => file.DeletedAtUtc == null)
                on series.CoverFileId equals cover.FileResourceId
                into coverGroup
            from cover in coverGroup.DefaultIfEmpty()
            where ranking.PublicationPeriodId == publicationPeriodId
            select new
            {
                Ranking = ranking,
                Series = series,
                Cover = cover
            };

        var normalizedSearch = NormalizeSearch(searchText);

        if (normalizedSearch is not null)
        {
            query = query.Where(row =>
                row.Ranking.Title.Contains(normalizedSearch));
        }

        query = sort switch
        {
            "rank_desc" =>
                query.OrderByDescending(row =>
                    row.Ranking.RankPosition),

            "title_asc" =>
                query.OrderBy(row =>
                    row.Ranking.Title),

            "title_desc" =>
                query.OrderByDescending(row =>
                    row.Ranking.Title),

            "score_desc" =>
                query.OrderByDescending(row =>
                    row.Ranking.RankingScore),

            "score_asc" =>
                query.OrderBy(row =>
                    row.Ranking.RankingScore),

            "average_rating_desc" =>
                query.OrderByDescending(row =>
                    row.Ranking.AverageRating),

            "average_rating_asc" =>
                query.OrderBy(row =>
                    row.Ranking.AverageRating),

            "reading_count_desc" =>
                query.OrderByDescending(row =>
                    row.Ranking.ReadingCount),

            "reading_count_asc" =>
                query.OrderBy(row =>
                    row.Ranking.ReadingCount),

            "rating_count_desc" =>
                query.OrderByDescending(row =>
                    row.Ranking.RatingCount),

            "rating_count_asc" =>
                query.OrderBy(row =>
                    row.Ranking.RatingCount),

            _ => query.OrderBy(row =>
                row.Ranking.RankPosition)
        };

        return await query
            .Select(row => new SeriesRankingRowDto(
                row.Ranking.PublicationPeriodId,
                row.Ranking.PeriodName,
                row.Ranking.PeriodTypeCode,
                row.Ranking.PeriodStartDate,
                row.Ranking.PeriodEndDate,
                row.Ranking.SeriesId,
                row.Ranking.Title,
                row.Ranking.Slug,
                row.Cover == null
                    ? null
                    : row.Cover.CloudinarySecureUrl,
                row.Ranking.RatingCount,
                row.Ranking.AverageRating,
                row.Ranking.ReadingCount,
                row.Ranking.RankingScore,

                // DTO currently uses int, while DENSE_RANK returns BIGINT.
                (int)row.Ranking.RankPosition))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RankableSeriesDto>>
        SearchRankableSeriesAsync(
            Guid publicationPeriodId,
            string? searchText,
            int maxResults,
            CancellationToken cancellationToken)
    {
        var normalizedSearch = NormalizeSearch(searchText);

        var safeMaxResults = maxResults <= 0
            ? 20
            : maxResults;

        var query =
            from series in _context.Set<Series>().AsNoTracking()
            join cover in _context.Set<FileResource>()
                    .AsNoTracking()
                    .Where(file => file.DeletedAtUtc == null)
                on series.CoverFileId equals cover.FileResourceId
                into coverGroup
            from cover in coverGroup.DefaultIfEmpty()
            where series.StatusCode == "SERIALIZED"
            select new
            {
                Series = series,
                Cover = cover
            };

        if (normalizedSearch is not null)
        {
            query = query.Where(row =>
                row.Series.Title.Contains(normalizedSearch));
        }

        return await query
            .OrderBy(row => row.Series.Title)
            .Take(safeMaxResults)
            .Select(row => new RankableSeriesDto(
                row.Series.SeriesId,
                row.Series.Title,
                row.Series.Slug,
                row.Cover == null
                    ? null
                    : row.Cover.CloudinarySecureUrl))
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
                user =>
                    user.UserId == actorUserId
                    && user.StatusCode == "ACTIVE"
                    && user.Role != null
                    && (
                        user.Role.RoleName == "Editorial Board Member"
                        || user.Role.RoleName == "EditorialBoardMember"
                        || user.Role.RoleName == "Board Member"
                        || user.Role.RoleName == "Editorial Board Chief"
                        || user.Role.RoleName == "EditorialBoardChief"
                        || user.Role.RoleName == "Board Chief"
                    ),
                cancellationToken);
    }

    public async Task<SeriesVoteInputDto>
        CreateSeriesVoteInputAsync(
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
            .AsNoTracking()
            .AnyAsync(
                period =>
                    period.PublicationPeriodId == publicationPeriodId,
                cancellationToken);

        if (!periodExists)
        {
            throw new InvalidOperationException(
                "Publication period was not found.");
        }

        var seriesExists = await _context.Set<Series>()
            .AsNoTracking()
            .AnyAsync(
                series =>
                    series.SeriesId == seriesId
                    && series.StatusCode == "SERIALIZED",
                cancellationToken);

        if (!seriesExists)
        {
            throw new InvalidOperationException(
                "Series was not found or is not currently serialized.");
        }

        var duplicateExists = await _context.Set<SeriesVoteInput>()
            .AsNoTracking()
            .AnyAsync(
                input =>
                    input.PublicationPeriodId == publicationPeriodId
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
            EnteredAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        _context.Set<SeriesVoteInput>().Add(input);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRequiredVoteInputAsync(
            input.SeriesVoteInputId,
            cancellationToken);
    }

    public async Task<SeriesVoteInputDto>
        UpdateSeriesVoteInputAsync(
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
                item =>
                    item.SeriesVoteInputId == seriesVoteInputId,
                cancellationToken);

        if (input is null)
        {
            throw new InvalidOperationException(
                "Series vote input was not found.");
        }

        input.RatingCount = ratingCount;
        input.AverageRating = averageRating;
        input.ReadingCount = readingCount;
        input.DataSourceNote = dataSourceNote;
        input.UpdatedByUserId = actorUserId;
        input.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRequiredVoteInputAsync(
            seriesVoteInputId,
            cancellationToken);
    }

    private async Task<SeriesVoteInputDto>
        GetRequiredVoteInputAsync(
            Guid seriesVoteInputId,
            CancellationToken cancellationToken)
    {
        var result = await (
            from input in _context.Set<SeriesVoteInput>()
                .AsNoTracking()
            join series in _context.Set<Series>().AsNoTracking()
                on input.SeriesId equals series.SeriesId
            join cover in _context.Set<FileResource>()
                    .AsNoTracking()
                    .Where(file => file.DeletedAtUtc == null)
                on series.CoverFileId equals cover.FileResourceId
                into coverGroup
            from cover in coverGroup.DefaultIfEmpty()
            where input.SeriesVoteInputId == seriesVoteInputId
            select new SeriesVoteInputDto(
                input.SeriesVoteInputId,
                input.PublicationPeriodId,
                input.SeriesId,
                series.Title,
                series.Slug,
                cover == null
                    ? null
                    : cover.CloudinarySecureUrl,
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

    private static string? NormalizeSearch(
        string? searchText)
    {
        var normalized = searchText?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}