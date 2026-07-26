using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.Features.Ranking.Warnings;
using Xunit;

namespace MangaManagementSystem.RegressionTests;

public sealed class RankingWarningRegressionTests
{
    [Fact]
    public void DefaultOptions_PreserveExpectedEvaluationPolicy()
    {
        var options = new RankingWarningOptions();

        Assert.False(options.Enabled);
        Assert.Null(options.AbsoluteScoreThreshold);
        Assert.Equal(0.25m, options.BottomPercentile);
        Assert.Equal(3, options.ConsecutiveWeeklyPeriods);
        Assert.Equal(2, options.RequiredFailedPeriods);
        Assert.Equal(4, options.MinimumRankedSeriesPerPeriod);
        Assert.True(options.RequireLatestPeriodFailure);
        Assert.Equal(1440, options.EvaluationIntervalMinutes);
    }

    [Fact]
    public void ValidOptions_PassValidation()
    {
        var options = new RankingWarningOptions
        {
            Enabled = true,
            AbsoluteScoreThreshold = 6.5m
        };

        var isValid = options.TryValidate(out var errorCode);

        Assert.True(isValid);
        Assert.Null(errorCode);
    }

    [Fact]
    public void MissingThreshold_IsRejected()
    {
        var options = new RankingWarningOptions
        {
            Enabled = true,
            AbsoluteScoreThreshold = null
        };

        var isValid = options.TryValidate(out var errorCode);

        Assert.False(isValid);
        Assert.Equal(
            "absolute_score_threshold_invalid",
            errorCode);
    }

    [Fact]
    public void NotificationContracts_RemainStable()
    {
        Assert.Equal(
            "RANKING_WARNING",
            NotificationTypeCodes.RankingWarning);

        Assert.Equal(
            "Series",
            NotificationRelatedEntityTypes.Series);

        Assert.Equal(
            "RankingWarning",
            RankingWarningOptions.SectionName);
    }
}