namespace MangaManagementSystem.Application.Features.Ranking.Warnings;

public sealed class RankingWarningOptions
{
    public const string SectionName = "RankingWarning";

    public bool Enabled { get; set; }

    public decimal? AbsoluteScoreThreshold { get; set; }

    public decimal BottomPercentile { get; set; } = 0.25m;

    public int ConsecutiveWeeklyPeriods { get; set; } = 3;

    public int RequiredFailedPeriods { get; set; } = 2;

    public int MinimumRankedSeriesPerPeriod { get; set; } = 4;

    public bool RequireLatestPeriodFailure { get; set; } = true;

    public int EvaluationIntervalMinutes { get; set; } = 1440;

    public bool TryValidate(out string? errorCode)
    {
        if (!AbsoluteScoreThreshold.HasValue
            || AbsoluteScoreThreshold.Value < 0m
            || AbsoluteScoreThreshold.Value > 10m)
        {
            errorCode = "absolute_score_threshold_invalid";
            return false;
        }

        if (BottomPercentile <= 0m || BottomPercentile > 1m)
        {
            errorCode = "bottom_percentile_invalid";
            return false;
        }

        if (ConsecutiveWeeklyPeriods <= 0)
        {
            errorCode = "consecutive_weekly_periods_invalid";
            return false;
        }

        if (RequiredFailedPeriods <= 0
            || RequiredFailedPeriods > ConsecutiveWeeklyPeriods)
        {
            errorCode = "required_failed_periods_invalid";
            return false;
        }

        if (MinimumRankedSeriesPerPeriod < 1)
        {
            errorCode = "minimum_ranked_series_invalid";
            return false;
        }

        if (EvaluationIntervalMinutes < 1)
        {
            errorCode = "evaluation_interval_invalid";
            return false;
        }

        errorCode = null;
        return true;
    }
}
