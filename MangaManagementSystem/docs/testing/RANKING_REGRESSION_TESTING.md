# Ranking Regression Testing

## Purpose

This suite verifies that Ranking Warning changes do not break existing
application behavior and that important contracts remain stable.

## Scope

The regression command runs:

1. Full solution build.
2. Existing Application tests.
3. Dedicated Ranking regression contract tests.

## Current coverage

- Ranking Warning default configuration.
- Valid configuration acceptance.
- Missing threshold rejection.
- Stable notification type code.
- Stable related entity type.
- Existing Ranking Warning evaluator behavior.
- Duplicate notification prevention.
- Contributor eligibility.
- Weekly period boundary behavior.

## Run command

From the solution directory, run:

    .\scripts\run-ranking-regression.ps1

Release mode is used by default.

Run without rebuilding:

    .\scripts\run-ranking-regression.ps1 -NoBuild

## Baseline before setup

- Full solution build passed with 0 errors.
- Existing automated tests: 21 passed.
- Failed: 0.
- Skipped: 0.