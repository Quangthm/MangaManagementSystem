using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Ranking.Warnings;

public sealed class RankingWarningEvaluator : IRankingWarningEvaluator
{
    private const string SerializedStatus = "SERIALIZED";
    private const string HiatusStatus = "HIATUS";
    private const string NotificationTitle = "Ranking performance warning";

    private readonly IRankingWarningRepository _repository;
    private readonly RankingWarningOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly RankingWarningEvaluationGate _evaluationGate;
    private readonly ILogger<RankingWarningEvaluator> _logger;

    public RankingWarningEvaluator(
        IRankingWarningRepository repository,
        RankingWarningOptions options,
        TimeProvider timeProvider,
        RankingWarningEvaluationGate evaluationGate,
        ILogger<RankingWarningEvaluator> logger)
    {
        _repository = repository;
        _options = options;
        _timeProvider = timeProvider;
        _evaluationGate = evaluationGate;
        _logger = logger;
    }

    public async Task<RankingWarningEvaluationSummary> EvaluateAsync(
        RankingWarningEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _evaluationGate.Semaphore.WaitAsync(cancellationToken);

        try
        {
            return await EvaluateCoreAsync(request, cancellationToken);
        }
        finally
        {
            _evaluationGate.Semaphore.Release();
        }
    }

    private async Task<RankingWarningEvaluationSummary> EvaluateCoreAsync(
        RankingWarningEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        var effectiveUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var trigger = NormalizeTrigger(request.Trigger);

        _logger.LogInformation(
            "Ranking warning batch_start. Trigger={Trigger}, EffectiveUtc={EffectiveUtc}, SeriesId={SeriesId}",
            trigger,
            effectiveUtc,
            request.SeriesId);

        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Ranking warning skipped. Reason=disabled, Trigger={Trigger}",
                trigger);

            return RankingWarningEvaluationSummary.Skipped(
                "disabled",
                trigger,
                effectiveUtc);
        }

        if (!_options.TryValidate(out var configurationError))
        {
            _logger.LogWarning(
                "Ranking warning skipped. Reason=config_invalid, Detail={Detail}",
                configurationError);

            return RankingWarningEvaluationSummary.Skipped(
                "config_invalid",
                trigger,
                effectiveUtc,
                configurationError);
        }

        var periods = await _repository
            .GetLatestCompletedWeeklyPeriodsAsync(
                effectiveUtc,
                _options.ConsecutiveWeeklyPeriods,
                cancellationToken);

        if (periods.Count != _options.ConsecutiveWeeklyPeriods)
        {
            return RankingWarningEvaluationSummary.Skipped(
                "incomplete_periods",
                trigger,
                effectiveUtc,
                $"Expected {_options.ConsecutiveWeeklyPeriods} completed weekly periods but found {periods.Count}.",
                periods.Select(period => period.PublicationPeriodId).ToArray());
        }

        var orderedPeriods = periods
            .OrderBy(period => period.PeriodStartDate)
            .ToArray();

        if (!AreConsecutive(orderedPeriods))
        {
            return RankingWarningEvaluationSummary.Skipped(
                "non_consecutive_periods",
                trigger,
                effectiveUtc,
                "The latest completed weekly periods are not contiguous.",
                orderedPeriods.Select(period => period.PublicationPeriodId).ToArray());
        }

        var periodIds = orderedPeriods
            .Select(period => period.PublicationPeriodId)
            .ToArray();

        var rows = await _repository.GetRankingRowsAsync(
            periodIds,
            cancellationToken);

        var rowCountsByPeriod = rows
            .GroupBy(row => row.PublicationPeriodId)
            .ToDictionary(group => group.Key, group => group.Count());

        var insufficientCohortPeriod = orderedPeriods.FirstOrDefault(
            period =>
                !rowCountsByPeriod.TryGetValue(
                    period.PublicationPeriodId,
                    out var rowCount)
                || rowCount < _options.MinimumRankedSeriesPerPeriod);

        if (insufficientCohortPeriod is not null)
        {
            return RankingWarningEvaluationSummary.Skipped(
                "insufficient_cohort",
                trigger,
                effectiveUtc,
                $"Period {insufficientCohortPeriod.PublicationPeriodId:D} has fewer than {_options.MinimumRankedSeriesPerPeriod} ranked series.",
                periodIds);
        }

        var latestPeriod = orderedPeriods[^1];
        var threshold = _options.AbsoluteScoreThreshold!.Value;
        var rowsBySeries = rows
            .Where(row => !request.SeriesId.HasValue || row.SeriesId == request.SeriesId.Value)
            .GroupBy(row => row.SeriesId)
            .ToArray();

        var candidateSeriesIds = new List<Guid>();
        var missingRowsSkipped = 0;

        foreach (var seriesRows in rowsBySeries)
        {
            var rowsByPeriod = seriesRows
                .GroupBy(row => row.PublicationPeriodId)
                .ToDictionary(group => group.Key, group => group.First());

            if (periodIds.Any(periodId => !rowsByPeriod.ContainsKey(periodId)))
            {
                missingRowsSkipped++;
                _logger.LogInformation(
                    "Ranking warning skipped. Reason=missing_row, SeriesId={SeriesId}",
                    seriesRows.Key);
                continue;
            }

            var failedPeriods = 0;
            var latestPeriodFailed = false;

            foreach (var period in orderedPeriods)
            {
                var row = rowsByPeriod[period.PublicationPeriodId];
                var totalRankedSeries = rowCountsByPeriod[period.PublicationPeriodId];
                var lowGroupSize = (int)Math.Ceiling(
                    totalRankedSeries * _options.BottomPercentile);

                var inBottomGroup =
                    row.RankPosition
                    > totalRankedSeries - lowGroupSize;

                var failed =
                    row.RankingScore < threshold
                    && inBottomGroup;

                if (failed)
                {
                    failedPeriods++;
                }

                if (period.PublicationPeriodId == latestPeriod.PublicationPeriodId)
                {
                    latestPeriodFailed = failed;
                }
            }

            if (failedPeriods < _options.RequiredFailedPeriods)
            {
                continue;
            }

            if (_options.RequireLatestPeriodFailure && !latestPeriodFailed)
            {
                continue;
            }

            candidateSeriesIds.Add(seriesRows.Key);

            _logger.LogInformation(
                "Ranking warning candidate. SeriesId={SeriesId}, FailedPeriods={FailedPeriods}, LatestPeriodId={LatestPeriodId}, Scores={Scores}",
                seriesRows.Key,
                failedPeriods,
                latestPeriod.PublicationPeriodId,
                string.Join(
                    ",",
                    seriesRows
                        .OrderBy(row => row.PublicationPeriodId)
                        .Select(row => row.RankingScore)));
        }

        if (candidateSeriesIds.Count == 0)
        {
            return new RankingWarningEvaluationSummary(
                "completed",
                trigger,
                effectiveUtc,
                periodIds,
                rowsBySeries.Length,
                0,
                0,
                0,
                missingRowsSkipped,
                0,
                0,
                0);
        }

        var seriesData = await _repository.GetSeriesAsync(
            candidateSeriesIds,
            cancellationToken);

        var seriesById = seriesData.ToDictionary(series => series.SeriesId);
        var notificationsCreated = 0;
        var duplicatesSkipped = 0;
        var statusSkipped = 0;
        var noRecipientSkipped = 0;
        var errorCount = 0;
        var eligibleCandidateCount = 0;
        var evaluationWindowStartUtc = latestPeriod.PeriodEndDate.Date.AddDays(1);

        foreach (var seriesId in candidateSeriesIds)
        {
            if (!seriesById.TryGetValue(seriesId, out var series)
                || !IsEligibleStatus(series.StatusCode))
            {
                statusSkipped++;
                _logger.LogInformation(
                    "Ranking warning skipped. Reason=status, SeriesId={SeriesId}, Status={Status}",
                    seriesId,
                    series?.StatusCode);
                continue;
            }

            eligibleCandidateCount++;

            IReadOnlyList<Guid> recipients;

            try
            {
                recipients = await _repository
                    .GetDistinctActiveContributorUserIdsAsync(
                        seriesId,
                        effectiveUtc,
                        cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errorCount++;
                _logger.LogError(
                    exception,
                    "Ranking warning recipient resolution failed. SeriesId={SeriesId}",
                    seriesId);
                continue;
            }

            var distinctRecipients = recipients
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray();

            if (distinctRecipients.Length == 0)
            {
                noRecipientSkipped++;
                _logger.LogInformation(
                    "Ranking warning skipped. Reason=no_recipient, SeriesId={SeriesId}",
                    seriesId);
                continue;
            }

            var message = BuildMessage(series.SeriesTitle);

            foreach (var recipientUserId in distinctRecipients)
            {
                try
                {
                    var duplicateExists = await _repository
                        .RankingWarningExistsAsync(
                            recipientUserId,
                            seriesId,
                            evaluationWindowStartUtc,
                            effectiveUtc,
                            cancellationToken);

                    if (duplicateExists)
                    {
                        duplicatesSkipped++;
                        _logger.LogInformation(
                            "Ranking warning skipped. Reason=duplicate, SeriesId={SeriesId}, RecipientUserId={RecipientUserId}, LatestPeriodId={LatestPeriodId}",
                            seriesId,
                            recipientUserId,
                            latestPeriod.PublicationPeriodId);
                        continue;
                    }

                    await _repository.AddRankingWarningAsync(
                        recipientUserId,
                        seriesId,
                        NotificationTitle,
                        message,
                        effectiveUtc,
                        cancellationToken);

                    await _repository.SaveChangesAsync(cancellationToken);
                    notificationsCreated++;

                    _logger.LogInformation(
                        "Ranking warning sent. SeriesId={SeriesId}, RecipientUserId={RecipientUserId}, LatestPeriodId={LatestPeriodId}",
                        seriesId,
                        recipientUserId,
                        latestPeriod.PublicationPeriodId);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    errorCount++;
                    _logger.LogError(
                        exception,
                        "Ranking warning notification failed. SeriesId={SeriesId}, RecipientUserId={RecipientUserId}",
                        seriesId,
                        recipientUserId);
                }
            }
        }

        var summary = new RankingWarningEvaluationSummary(
            "completed",
            trigger,
            effectiveUtc,
            periodIds,
            rowsBySeries.Length,
            eligibleCandidateCount,
            notificationsCreated,
            duplicatesSkipped,
            missingRowsSkipped,
            statusSkipped,
            noRecipientSkipped,
            errorCount,
            "Application-level dedup is idempotent for a single instance but is not a hard multi-instance guarantee.");

        _logger.LogInformation(
            "Ranking warning batch_end. Trigger={Trigger}, Candidates={Candidates}, Sent={Sent}, Duplicates={Duplicates}, MissingRows={MissingRows}, StatusSkipped={StatusSkipped}, NoRecipient={NoRecipient}, Errors={Errors}",
            trigger,
            summary.CandidateSeriesCount,
            summary.NotificationsCreated,
            summary.DuplicateNotificationsSkipped,
            summary.MissingRowsSkipped,
            summary.StatusSkipped,
            summary.NoRecipientSkipped,
            summary.ErrorCount);

        return summary;
    }

    private static bool AreConsecutive(
        IReadOnlyList<RankingWarningPeriodData> periods)
    {
        for (var index = 1; index < periods.Count; index++)
        {
            var previousEndDate = periods[index - 1].PeriodEndDate.Date;
            var currentStartDate = periods[index].PeriodStartDate.Date;

            if (currentStartDate != previousEndDate.AddDays(1))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsEligibleStatus(
        string? statusCode)
    {
        return string.Equals(
                statusCode,
                SerializedStatus,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                statusCode,
                HiatusStatus,
                StringComparison.OrdinalIgnoreCase);
    }

    private string BuildMessage(
        string seriesTitle)
    {
        return $"\"{seriesTitle}\" met the ranking warning rule in at least {_options.RequiredFailedPeriods} of the latest {_options.ConsecutiveWeeklyPeriods} completed weekly periods, including the latest week. Please review its recent rating and ranking performance.";
    }

    private static string NormalizeTrigger(
        string? trigger)
    {
        return string.IsNullOrWhiteSpace(trigger)
            ? "unknown"
            : trigger.Trim();
    }
}
