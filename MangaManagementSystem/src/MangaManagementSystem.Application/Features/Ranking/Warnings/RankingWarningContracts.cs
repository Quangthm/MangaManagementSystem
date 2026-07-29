namespace MangaManagementSystem.Application.Features.Ranking.Warnings;

public static class RankingWarningEvaluationTriggers
{
    public const string Scheduler = "scheduler";
    public const string CorrectionCatchUp = "correction_catch_up";
    public const string Manual = "manual";
}

public sealed record RankingWarningEvaluationRequest(
    string Trigger,
    Guid? SeriesId = null);

public sealed record RankingWarningEvaluationSummary(
    string OutcomeCode,
    string Trigger,
    DateTime EffectiveUtc,
    IReadOnlyList<Guid> EvaluatedPeriodIds,
    int SeriesConsidered,
    int CandidateSeriesCount,
    int NotificationsCreated,
    int DuplicateNotificationsSkipped,
    int MissingRowsSkipped,
    int StatusSkipped,
    int NoRecipientSkipped,
    int ErrorCount,
    string? Detail = null)
{
    public static RankingWarningEvaluationSummary Skipped(
        string outcomeCode,
        string trigger,
        DateTime effectiveUtc,
        string? detail = null,
        IReadOnlyList<Guid>? evaluatedPeriodIds = null)
    {
        return new RankingWarningEvaluationSummary(
            outcomeCode,
            trigger,
            effectiveUtc,
            evaluatedPeriodIds ?? Array.Empty<Guid>(),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            detail);
    }
}

public sealed record RankingWarningPeriodData(
    Guid PublicationPeriodId,
    string PeriodName,
    DateTime PeriodStartDate,
    DateTime PeriodEndDate);

public sealed record RankingWarningRowData(
    Guid PublicationPeriodId,
    Guid SeriesId,
    string SeriesTitle,
    decimal RankingScore,
    long RankPosition);

public sealed record RankingWarningSeriesData(
    Guid SeriesId,
    string SeriesTitle,
    string StatusCode);

public sealed class RankingWarningEvaluationGate
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}

public interface IRankingWarningEvaluator
{
    Task<RankingWarningEvaluationSummary> EvaluateAsync(
        RankingWarningEvaluationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IRankingWarningRepository
{
    Task<IReadOnlyList<RankingWarningPeriodData>>
        GetLatestCompletedWeeklyPeriodsAsync(
            DateTime effectiveUtc,
            int count,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<RankingWarningRowData>> GetRankingRowsAsync(
        IReadOnlyCollection<Guid> publicationPeriodIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RankingWarningSeriesData>> GetSeriesAsync(
        IReadOnlyCollection<Guid> seriesIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetDistinctActiveContributorUserIdsAsync(
        Guid seriesId,
        DateTime effectiveUtc,
        CancellationToken cancellationToken);

    Task<bool> RankingWarningExistsAsync(
        Guid recipientUserId,
        Guid seriesId,
        DateTime evaluationWindowStartUtc,
        DateTime effectiveUtc,
        CancellationToken cancellationToken);

    Task AddRankingWarningAsync(
        Guid recipientUserId,
        Guid seriesId,
        string title,
        string message,
        DateTime createdAtUtc,
        CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
