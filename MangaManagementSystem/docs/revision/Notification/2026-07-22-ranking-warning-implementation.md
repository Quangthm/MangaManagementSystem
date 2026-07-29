# RANKING_WARNING implementation handoff

**Baseline:** `4a691f1`
**Branch:** `feature/ranking-warning-implementation`
**Database change:** None

## Implemented contract

A series is eligible when all conditions hold:

- Feature is enabled and configuration is valid.
- Latest 3 consecutive completed WEEKLY periods exist. A period ending on date D becomes completed at D+1 00:00 UTC.
- Every evaluated period contains at least 4 ranked series.
- The series has a ranking row in all 3 periods.
- A failed week requires both `ranking_score < AbsoluteScoreThreshold` and bottom-25% position.
- At least 2 of 3 periods fail, including the latest period.
- Current status is `SERIALIZED` or `HIATUS`.
- Recipient is a distinct active contributor of that exact series with active user status.
- No notification exists for recipient + series in the latest evaluation window.
- A process-local evaluation gate serializes scheduler, catch-up, and manual runs to prevent single-instance races.

## Runtime flow

- Scheduled hosted service calls the shared evaluator.
- Create/update vote input calls the same evaluator after successful persistence; evaluator failure does not roll back ranking input.
- Development-only manual run calls the same evaluator.
- `RANKING_WARNING + Series` routes to `/ranking` in the Bell.

## Configuration

Production-safe baseline:

```json
"RankingWarning": {
  "Enabled": false,
  "AbsoluteScoreThreshold": null,
  "BottomPercentile": 0.25,
  "ConsecutiveWeeklyPeriods": 3,
  "RequiredFailedPeriods": 2,
  "MinimumRankedSeriesPerPeriod": 4,
  "RequireLatestPeriodFailure": true,
  "EvaluationIntervalMinutes": 1440
}
```

Development/test may set `AbsoluteScoreThreshold` to `6.5` and explicitly enable the feature.

## Known limitation

Dedup is query-before-insert using existing Notification columns. A process-local semaphore makes it idempotent inside one application instance, including concurrent scheduler/catch-up/manual runs. It is not a hard guarantee when multiple instances race. No unique constraint was added.
