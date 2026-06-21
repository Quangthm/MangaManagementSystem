# Series Slug Page Direct-Access Guard

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-19

## Problem

`/series/{slug}` could be accessed directly by URL regardless of `Series.StatusCode`. A user could manually navigate to a series in `PROPOSAL_DRAFT`, `UNDER_EDITORIAL_REVIEW`, or `UNDER_BOARD_REVIEW` status by entering the URL. The existing UI navigation lists already respected `SeriesNavigationPolicy`, but the read API did not.

## Read Path Inspected

```
SeriesPage.razor
  -> SeriesApiClient.GetSeriesDetailAsync(slug)
  -> GET /api/series/{slug}
  -> SeriesController.GetBySlugAsync
  -> GetSeriesBySlugQuery
  -> GetSeriesBySlugQueryHandler
  -> _seriesRepository.GetSeriesDetailBySlugAsync(slug)
     (filtered by slug only — no status check)
```

## Files Changed

- `src/MangaManagementSystem.Application/Features/Series/Queries/GetSeriesBySlug/GetSeriesBySlugQueryHandler.cs`
  - Injected `ISeriesProposalRepository`
  - Added latest-proposal fetch and `SeriesNavigationPolicy` check after series is loaded
  - Returns `null` (→ 404) if policy disallows access

## Files Not Changed

- `SeriesController.cs` — already maps `null` result to 404
- `SeriesApiClient.cs` — already maps 404 to `null`
- `SeriesPage.razor` — already handles `_series == null` (not-found state)
- `SeriesDetailDto.cs` — no new fields needed
- `ISeriesRepository.cs` / `SeriesRepository.cs` — no changes
- `SeriesNavigationPolicy.cs` — no changes
- No stored procedures changed

## Policy Enforced

`Application/Common/Policies/SeriesNavigationPolicy.cs` — `CanOpenSeriesSlugPage(seriesStatusCode, seriesSlug, latestProposalId, latestProposalStatusCode)`

## Latest Proposal Ordering Decision

`(SeriesId, ProposalVersionNo)` has a `UNIQUE` constraint in both the DB schema (`UQ_SeriesProposal_Series_VersionNo`) and EF configuration (`HasIndex(...).IsUnique()`). Therefore `ProposalVersionNo` is guaranteed unique per series. The existing `GetLatestBySeriesIdAsync` ordering by `ProposalVersionNo DESC` is deterministic — no repository change needed.

## Allowed Statuses

- `SERIALIZED`
- `HIATUS`
- `COMPLETED`
- `CANCELLED` — only if latest proposal exists and its `StatusCode` is `APPROVED`

## Disallowed Statuses (→ 404)

- `PROPOSAL_DRAFT`
- `UNDER_EDITORIAL_REVIEW`
- `UNDER_BOARD_REVIEW`
- `CANCELLED` without a latest proposal in `APPROVED` status

## Build Result

- **Errors:** 0
- **Warnings:** 60 (all pre-existing)

## Changed-File Warning Check

Command run:
```powershell
dotnet build MangaManagementSystem.sln --no-incremental 2>&1 |
    Select-String -Pattern "GetSeriesBySlug|SeriesRepository|SeriesNavigationPolicy|SeriesController|SeriesApiClient|SeriesPage"
```

Result: Only pre-existing `SeriesRepository.cs(29): warning CS0108` (`_context` hides inherited member) — present in baseline. No new warnings from `GetSeriesBySlugQueryHandler.cs` or any other changed file.

## Manual Test Checklist

- [ ] `SERIALIZED` series with slug opens `/series/{slug}`.
- [ ] `HIATUS` series with slug opens `/series/{slug}`.
- [ ] `COMPLETED` series with slug opens `/series/{slug}`.
- [ ] `CANCELLED` series with latest proposal `APPROVED` opens `/series/{slug}`.
- [ ] `CANCELLED` series without latest proposal `APPROVED` returns 404/not found.
- [ ] `PROPOSAL_DRAFT` series returns 404/not found when accessed directly by URL.
- [ ] `UNDER_EDITORIAL_REVIEW` series returns 404/not found when accessed directly by URL.
- [ ] `UNDER_BOARD_REVIEW` series returns 404/not found when accessed directly by URL.
- [ ] Editor Dashboard still routes non-slug-eligible series to proposal detail (no regression).
- [ ] Build: 0 errors, no new changed-file warnings.

## Remaining Tasks

1. **Data cleanup:** Inconsistent `PROPOSAL_DRAFT` series with `UNDER_EDITORIAL_REVIEW` proposal rows — stale/test data that should be reconciled.
2. **Cover crop-upload pipeline:** Implement crop-to-`1000×1500` before upload; no DB schema change needed.
