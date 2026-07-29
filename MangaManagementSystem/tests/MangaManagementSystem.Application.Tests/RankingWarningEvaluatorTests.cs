using MangaManagementSystem.Application.Features.Ranking.Warnings;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MangaManagementSystem.Application.Tests;

public sealed class RankingWarningEvaluatorTests
{
    private static readonly DateTime EffectiveUtc =
        new(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FeatureDisabled_SkipsWithoutQueryingData()
    {
        var repository = new FakeRankingWarningRepository();
        var evaluator = new RankingWarningEvaluator(
            repository,
            new RankingWarningOptions
            {
                Enabled = false
            },
            new FixedTimeProvider(EffectiveUtc),
            new RankingWarningEvaluationGate(),
            NullLogger<RankingWarningEvaluator>.Instance);

        var summary = await evaluator.EvaluateAsync(
            new RankingWarningEvaluationRequest(
                RankingWarningEvaluationTriggers.Scheduler));

        Assert.Equal("disabled", summary.OutcomeCode);
        Assert.Empty(repository.Notifications);
    }

    [Fact]
    public async Task EnabledWithMissingThreshold_ReturnsConfigInvalid()
    {
        var repository = new FakeRankingWarningRepository();
        var evaluator = new RankingWarningEvaluator(
            repository,
            new RankingWarningOptions
            {
                Enabled = true,
                AbsoluteScoreThreshold = null
            },
            new FixedTimeProvider(EffectiveUtc),
            new RankingWarningEvaluationGate(),
            NullLogger<RankingWarningEvaluator>.Instance);

        var summary = await evaluator.EvaluateAsync(
            new RankingWarningEvaluationRequest(
                RankingWarningEvaluationTriggers.Manual));

        Assert.Equal("config_invalid", summary.OutcomeCode);
        Assert.Empty(repository.Notifications);
    }

    [Fact]
    public async Task BottomSeriesWithGoodScore_DoesNotCreateWarning()
    {
        var fixture = CreateFixture();
        fixture.SetScores(8m, 8m, 8m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(0, summary.NotificationsCreated);
    }

    [Fact]
    public async Task FailsTwoOfThreeIncludingLatest_CreatesWarning()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 7m, 6.3m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(1, summary.NotificationsCreated);
        Assert.Single(fixture.Repository.Notifications);
    }

    [Fact]
    public async Task BottomPercentileButAboveThreshold_DoesNotCreateWarning()
    {
        var fixture = CreateFixture();
        fixture.SetScores(7m, 7m, 7m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(0, summary.NotificationsCreated);
    }

    [Fact]
    public async Task OnlyOneFailedWeek_DoesNotCreateWarning()
    {
        var fixture = CreateFixture();
        fixture.SetScores(7m, 7m, 6.4m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(0, summary.NotificationsCreated);
    }

    [Fact]
    public async Task TwoFailedWeeksWithoutLatestFailure_DoesNotCreateWarning()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 6.3m, 7m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(0, summary.NotificationsCreated);
    }

    [Fact]
    public async Task MissingRankingRow_DoesNotCreateWarning()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);
        fixture.Repository.Rows.RemoveAt(0);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Equal(1, summary.MissingRowsSkipped);
    }

    [Fact]
    public async Task ScoreEqualToThreshold_DoesNotCountAsFailure()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.5m, 7m, 6.5m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(0, summary.NotificationsCreated);
    }

    [Theory]
    [InlineData("SERIALIZED", 1)]
    [InlineData("HIATUS", 1)]
    [InlineData("CANCELLED", 0)]
    [InlineData("COMPLETED", 0)]
    public async Task CurrentSeriesStatusControlsEligibility(
        string statusCode,
        int expectedNotifications)
    {
        var fixture = CreateFixture(statusCode);
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(expectedNotifications, summary.NotificationsCreated);
    }

    [Fact]
    public async Task DistinctActiveContributors_ReceiveOneNotificationEach()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);
        var secondRecipient = Guid.NewGuid();
        fixture.Repository.Recipients =
        [
            fixture.RecipientUserId,
            fixture.RecipientUserId,
            secondRecipient
        ];

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(2, summary.NotificationsCreated);
        Assert.Equal(2, fixture.Repository.Notifications.Count);
    }

    [Fact]
    public async Task NoRecipient_SkipsSeriesAndContinues()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);
        fixture.Repository.Recipients = [];

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Equal(1, summary.NoRecipientSkipped);
    }

    [Fact]
    public async Task InactiveOrEndedContributors_ProduceNoEligibleRecipient()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);

        // The EF repository filters inactive users and ended contributor
        // relationships before returning IDs to the evaluator.
        fixture.Repository.Recipients = [];

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Equal(1, summary.NoRecipientSkipped);
    }

    [Fact]
    public async Task ConcurrentTriggersInOneInstance_DoNotDuplicate()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);

        var summaries = await Task.WhenAll(
            fixture.Evaluator.EvaluateAsync(
                new RankingWarningEvaluationRequest(
                    RankingWarningEvaluationTriggers.Scheduler)),
            fixture.Evaluator.EvaluateAsync(
                new RankingWarningEvaluationRequest(
                    RankingWarningEvaluationTriggers.CorrectionCatchUp)),
            fixture.Evaluator.EvaluateAsync(
                new RankingWarningEvaluationRequest(
                    RankingWarningEvaluationTriggers.Manual)));

        Assert.Single(fixture.Repository.Notifications);
        Assert.Equal(1, summaries.Sum(summary => summary.NotificationsCreated));
        Assert.Equal(2, summaries.Sum(summary => summary.DuplicateNotificationsSkipped));
    }

    [Fact]
    public async Task SchedulerCatchUpAndManualRunSameWindow_DoNotDuplicate()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);

        await fixture.Evaluator.EvaluateAsync(
            new RankingWarningEvaluationRequest(
                RankingWarningEvaluationTriggers.Scheduler));
        await fixture.Evaluator.EvaluateAsync(
            new RankingWarningEvaluationRequest(
                RankingWarningEvaluationTriggers.CorrectionCatchUp));
        var summary = await fixture.Evaluator.EvaluateAsync(
            new RankingWarningEvaluationRequest(
                RankingWarningEvaluationTriggers.Manual));

        Assert.Single(fixture.Repository.Notifications);
        Assert.Equal(1, summary.DuplicateNotificationsSkipped);
    }

    [Fact]
    public async Task PeriodEndDayAt235959_IsNotCompleted()
    {
        var fixture = CreateFixture(
            effectiveUtc: new DateTime(
                2026,
                7,
                26,
                23,
                59,
                59,
                DateTimeKind.Utc));
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal("incomplete_periods", summary.OutcomeCode);
    }

    [Fact]
    public async Task NextDayAtMidnight_PeriodIsCompleted()
    {
        var fixture = CreateFixture();
        fixture.SetScores(6.4m, 6.3m, 6.2m, rankPosition: 4);

        var summary = await fixture.EvaluateAsync();

        Assert.Equal(1, summary.NotificationsCreated);
    }

    [Fact]
    public void ResetAndNewProvider_ReturnToRealTimeMode()
    {
        var provider = new DevelopmentTimeProvider();
        provider.SetFixedUtc(
            new DateTimeOffset(
                2030,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero));

        Assert.True(provider.GetState().IsFakeTimeActive);
        Assert.False(provider.Reset().IsFakeTimeActive);
        Assert.False(new DevelopmentTimeProvider().GetState().IsFakeTimeActive);
    }

    private static Fixture CreateFixture(
        string statusCode = "SERIALIZED",
        DateTime? effectiveUtc = null)
    {
        var repository = new FakeRankingWarningRepository();
        var timeProvider = new FixedTimeProvider(
            effectiveUtc ?? EffectiveUtc);
        var seriesId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();

        repository.Series =
        [
            new RankingWarningSeriesData(
                seriesId,
                "Test Series",
                statusCode)
        ];
        repository.Recipients = [recipientUserId];

        var evaluator = new RankingWarningEvaluator(
            repository,
            new RankingWarningOptions
            {
                Enabled = true,
                AbsoluteScoreThreshold = 6.5m,
                BottomPercentile = 0.25m,
                ConsecutiveWeeklyPeriods = 3,
                RequiredFailedPeriods = 2,
                MinimumRankedSeriesPerPeriod = 4,
                RequireLatestPeriodFailure = true,
                EvaluationIntervalMinutes = 1440
            },
            timeProvider,
            new RankingWarningEvaluationGate(),
            NullLogger<RankingWarningEvaluator>.Instance);

        return new Fixture(
            evaluator,
            repository,
            seriesId,
            recipientUserId);
    }

    private sealed record Fixture(
        RankingWarningEvaluator Evaluator,
        FakeRankingWarningRepository Repository,
        Guid SeriesId,
        Guid RecipientUserId)
    {
        public Task<RankingWarningEvaluationSummary> EvaluateAsync()
        {
            return Evaluator.EvaluateAsync(
                new RankingWarningEvaluationRequest(
                    RankingWarningEvaluationTriggers.Manual));
        }

        public void SetScores(
            decimal first,
            decimal second,
            decimal third,
            long rankPosition)
        {
            Repository.SetSeriesRows(
                SeriesId,
                first,
                second,
                third,
                rankPosition);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(
                DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed class FakeRankingWarningRepository
        : IRankingWarningRepository
    {
        private readonly Guid[] _periodIds =
        [
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        ];

        private readonly Guid[] _competitorSeriesIds =
        [
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        ];

        public FakeRankingWarningRepository()
        {
            Periods =
            [
                new RankingWarningPeriodData(
                    _periodIds[0],
                    "Week 1",
                    new DateTime(2026, 7, 6),
                    new DateTime(2026, 7, 12)),
                new RankingWarningPeriodData(
                    _periodIds[1],
                    "Week 2",
                    new DateTime(2026, 7, 13),
                    new DateTime(2026, 7, 19)),
                new RankingWarningPeriodData(
                    _periodIds[2],
                    "Week 3",
                    new DateTime(2026, 7, 20),
                    new DateTime(2026, 7, 26))
            ];
        }

        public List<RankingWarningPeriodData> Periods { get; }

        public List<RankingWarningRowData> Rows { get; } = [];

        public IReadOnlyList<RankingWarningSeriesData> Series { get; set; } = [];

        public IReadOnlyList<Guid> Recipients { get; set; } = [];

        public List<CreatedNotification> Notifications { get; } = [];

        public void SetSeriesRows(
            Guid seriesId,
            decimal first,
            decimal second,
            decimal third,
            long rankPosition)
        {
            Rows.Clear();

            var scores = new[] { first, second, third };

            for (var periodIndex = 0; periodIndex < _periodIds.Length; periodIndex++)
            {
                Rows.Add(
                    new RankingWarningRowData(
                        _periodIds[periodIndex],
                        seriesId,
                        "Test Series",
                        scores[periodIndex],
                        rankPosition));

                for (var competitorIndex = 0; competitorIndex < _competitorSeriesIds.Length; competitorIndex++)
                {
                    Rows.Add(
                        new RankingWarningRowData(
                            _periodIds[periodIndex],
                            _competitorSeriesIds[competitorIndex],
                            $"Competitor {competitorIndex}",
                            8m + competitorIndex,
                            competitorIndex + 1));
                }
            }
        }

        public Task<IReadOnlyList<RankingWarningPeriodData>>
            GetLatestCompletedWeeklyPeriodsAsync(
                DateTime effectiveUtc,
                int count,
                CancellationToken cancellationToken)
        {
            IReadOnlyList<RankingWarningPeriodData> result = Periods
                .Where(period => period.PeriodEndDate < effectiveUtc.Date)
                .OrderByDescending(period => period.PeriodEndDate)
                .Take(count)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<RankingWarningRowData>> GetRankingRowsAsync(
            IReadOnlyCollection<Guid> publicationPeriodIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<RankingWarningRowData> result = Rows
                .Where(row => publicationPeriodIds.Contains(row.PublicationPeriodId))
                .ToArray();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<RankingWarningSeriesData>> GetSeriesAsync(
            IReadOnlyCollection<Guid> seriesIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<RankingWarningSeriesData> result = Series
                .Where(series => seriesIds.Contains(series.SeriesId))
                .ToArray();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Guid>> GetDistinctActiveContributorUserIdsAsync(
            Guid seriesId,
            DateTime effectiveUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Recipients);
        }

        public Task<bool> RankingWarningExistsAsync(
            Guid recipientUserId,
            Guid seriesId,
            DateTime evaluationWindowStartUtc,
            DateTime effectiveUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Notifications.Any(notification =>
                    notification.RecipientUserId == recipientUserId
                    && notification.SeriesId == seriesId
                    && notification.CreatedAtUtc >= evaluationWindowStartUtc
                    && notification.CreatedAtUtc <= effectiveUtc));
        }

        public Task AddRankingWarningAsync(
            Guid recipientUserId,
            Guid seriesId,
            string title,
            string message,
            DateTime createdAtUtc,
            CancellationToken cancellationToken)
        {
            Notifications.Add(
                new CreatedNotification(
                    recipientUserId,
                    seriesId,
                    createdAtUtc));
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed record CreatedNotification(
        Guid RecipientUserId,
        Guid SeriesId,
        DateTime CreatedAtUtc);
}
