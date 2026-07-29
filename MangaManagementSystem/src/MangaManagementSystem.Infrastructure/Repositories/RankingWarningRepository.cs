using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.Features.Ranking.Warnings;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories;

public sealed class RankingWarningRepository : IRankingWarningRepository
{
    private readonly ApplicationDbContext _context;

    public RankingWarningRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RankingWarningPeriodData>>
        GetLatestCompletedWeeklyPeriodsAsync(
            DateTime effectiveUtc,
            int count,
            CancellationToken cancellationToken)
    {
        var effectiveDate = effectiveUtc.Date;

        return await _context.PublicationPeriods
            .AsNoTracking()
            .Where(period =>
                period.PeriodTypeCode == "WEEKLY"
                && period.PeriodEndDate < effectiveDate)
            .OrderByDescending(period => period.PeriodEndDate)
            .ThenByDescending(period => period.PeriodStartDate)
            .Take(count)
            .Select(period => new RankingWarningPeriodData(
                period.PublicationPeriodId,
                period.PeriodName,
                period.PeriodStartDate,
                period.PeriodEndDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RankingWarningRowData>>
        GetRankingRowsAsync(
            IReadOnlyCollection<Guid> publicationPeriodIds,
            CancellationToken cancellationToken)
    {
        if (publicationPeriodIds.Count == 0)
        {
            return Array.Empty<RankingWarningRowData>();
        }

        return await _context.Set<SeriesRankingViewRow>()
            .AsNoTracking()
            .Where(row => publicationPeriodIds.Contains(row.PublicationPeriodId))
            .Select(row => new RankingWarningRowData(
                row.PublicationPeriodId,
                row.SeriesId,
                row.Title,
                row.RankingScore,
                row.RankPosition))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RankingWarningSeriesData>>
        GetSeriesAsync(
            IReadOnlyCollection<Guid> seriesIds,
            CancellationToken cancellationToken)
    {
        if (seriesIds.Count == 0)
        {
            return Array.Empty<RankingWarningSeriesData>();
        }

        return await _context.Series
            .AsNoTracking()
            .Where(series => seriesIds.Contains(series.SeriesId))
            .Select(series => new RankingWarningSeriesData(
                series.SeriesId,
                series.Title,
                series.StatusCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>>
        GetDistinctActiveContributorUserIdsAsync(
            Guid seriesId,
            DateTime effectiveUtc,
            CancellationToken cancellationToken)
    {
        return await (
            from contributor in _context.SeriesContributors.AsNoTracking()
            join user in _context.Users.AsNoTracking()
                on contributor.UserId equals user.UserId
            where contributor.SeriesId == seriesId
                  && contributor.EndDate == null
                  && contributor.StartDate <= effectiveUtc
                  && user.StatusCode == "ACTIVE"
            select user.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> RankingWarningExistsAsync(
        Guid recipientUserId,
        Guid seriesId,
        DateTime evaluationWindowStartUtc,
        DateTime effectiveUtc,
        CancellationToken cancellationToken)
    {
        return _context.Notifications
            .AsNoTracking()
            .AnyAsync(
                notification =>
                    notification.RecipientUserId == recipientUserId
                    && notification.NotificationTypeCode
                        == NotificationTypeCodes.RankingWarning
                    && notification.RelatedEntityType
                        == NotificationRelatedEntityTypes.Series
                    && notification.RelatedEntityId == seriesId
                    && notification.CreatedAtUtc >= evaluationWindowStartUtc
                    && notification.CreatedAtUtc <= effectiveUtc,
                cancellationToken);
    }

    public async Task AddRankingWarningAsync(
        Guid recipientUserId,
        Guid seriesId,
        string title,
        string message,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        await _context.Notifications.AddAsync(
            new Notification
            {
                RecipientUserId = recipientUserId,
                NotificationTypeCode = NotificationTypeCodes.RankingWarning,
                Title = title,
                Message = message,
                RelatedEntityType = NotificationRelatedEntityTypes.Series,
                RelatedEntityId = seriesId,
                CreatedAtUtc = createdAtUtc
            },
            cancellationToken);
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // This repository is used in a dedicated evaluation scope.
            // Clearing prevents one failed Added notification from poisoning
            // the remaining recipients in the same batch.
            _context.ChangeTracker.Clear();
            throw;
        }
    }
}
