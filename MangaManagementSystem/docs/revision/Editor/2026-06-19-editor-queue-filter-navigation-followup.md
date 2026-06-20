# Editor Queue Filter & Navigation Follow-up

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-19

## Summary

Final follow-up session completing the Tantou Editor pipeline. Fixed rendering bugs, added client-side search/filter to proposal and chapter review queues, created reusable navigation components, and ran full build verification with zero new warnings.

## Issues Fixed

### Proposal Queue Version Rendering
- `v@context.ProposalVersionNo` in `ProposalList.razor` and `EditorDashboard.razor` rendered verbatim instead of evaluating — fixed to `@($"v{context.ProposalVersionNo}")`.

### Proposal Queue Search/Filter
- Added client-side text search across series title, proposal title, and submitter name.
- Search is client-only; no server round-trip for filtering.
- Removed invalid `Size` attribute from `MudTextField` search field in `ProposalList.razor` because this MudBlazor version does not support `Size` on `MudTextField` and raises MUD0002.

### Chapter Review Queue Search/Filter
- Added client-side text search across series title, chapter number, and chapter title.
- Chapter status `MudSelect` filter width stabilized at `Style="width: 220px; min-width: 220px; max-width: 220px;"` — prevents layout shift when selected option text length varies.
- Removed invalid `Size` attribute from `MudTextField` search field in `ChapterReviewList.razor` because this MudBlazor version does not support `Size` on `MudTextField` and raises MUD0002.

### Dashboard Recent Activity Navigation
- `EditorDashboard.razor` Recent Series links now use `SeriesNavigationUrlBuilder` instead of raw string concatenation.
- Requires correct `CanOpenSeriesSlugPage` bool from DTO.

## Reusable Components

### `SeriesNavigationPolicy` (Application layer)
- `src/MangaManagementSystem.Application/Common/Policies/SeriesNavigationPolicy.cs`
- Business rule for slug-page eligibility:
  - `SERIALIZED`, `HIATUS`, `COMPLETED` always allowed.
  - `CANCELLED` allowed only if latest proposal is `APPROVED`.
  - All other statuses disallowed.
- Called only in Application handlers, never in Web layer.

### `SeriesNavigationUrlBuilder` (Web layer)
- `src/MangaManagementSystem.Web/Services/SeriesNavigationUrlBuilder.cs`
- Builds href for `/series/{slug}` from a DTO-provided `CanOpenSeriesSlugPage` bool.
- **Does not** call `SeriesNavigationPolicy` directly — slug eligibility is pre-computed in the handler and exposed via the DTO bool.
- Used by `EditorDashboard.razor` and `SeriesList.razor`.

### `SafeReturnUrl` (Web layer)
- `src/MangaManagementSystem.Web/Services/SafeReturnUrl.cs`
- Strict local-only return URL validator.
- Allows `/editor/` and `/series/` prefixes plus exact paths.
- Denies `javascript:`, protocol-relative, and external URLs.
- Used by `ProposalReviewDetail.razor`, `SeriesPage.razor`, `CreatorWorkspace.razor`.

## Dashboard Enrichment

- Dashboard recent series activity (≤5 series) enriched with proposal data using one batched `GetLatestForSeriesBatchAsync` call — no N+1.
- Each `EditorDashboardSeriesActivityDto` now includes `LatestProposalId`, `LatestProposalStatusCode`, `CanOpenSeriesSlugPage`.

## Cover Aspect-Ratio Decision

- **Future crop target:** `1000 × 1500` (2:3 portrait).
- **Aspect ratio:** 2:3 enforced in UI via `aspect-ratio: 2/3` with `object-fit: cover`.
- **Crop pipeline (MVP):** Cropped image uploaded as actual `SERIES_COVER` — no new file type or DB schema change needed.
- **UI:** Cover boxes must continue enforcing 2:3 aspect ratio even after crop-upload is implemented.

## Build Result

- **Errors:** 0
- **Warnings:** 60 (all pre-existing)
- **Changed-file warning check result:** No warnings from any changed/created file:
  - `ProposalList.razor` — 0 warnings
  - `ChapterReviewList.razor` — 0 warnings
  - `EditorDashboard.razor` — 0 warnings
  - `EditorDashboardDtos.cs` — 0 warnings
  - `SeriesNavigationPolicy.cs` — 0 warnings
  - `SeriesNavigationUrlBuilder.cs` — 0 warnings
  - `SafeReturnUrl.cs` — 0 warnings
  - `EditorSeriesDtos.cs` — 0 warnings
- **Pre-existing warnings (unchanged):** e.g. `CreateWorkspace.razor(1,1): warning MUD0002: Illegal Attribute 'DisableElevation' on 'MudButton'` — outside Editor scope, not addressed.

## Manual Test Checklist

- [ ] Proposal list renders version as `v1`, `v2`, etc.
- [ ] Proposal list text search filters by title and submitter.
- [ ] Chapter review list text search filters by series title, chapter number, chapter title.
- [ ] Chapter review status filter width stays stable (no layout shift).
- [ ] Dashboard Recent Series links navigate correctly based on eligibility.
- [ ] Series Library "View" / "View Series" links use `SeriesNavigationUrlBuilder`.
- [ ] Return URL navigation works from Proposal Review, Series Page, Creator Workspace.
- [ ] Status mismatch (series vs proposal) shows read-only warning, no action buttons.
- [ ] Synopsis toggle (View more / Show less) works on Proposal Review and Series Page.
- [ ] No new MUD0002 or other build warnings in changed files.

## Remaining Tasks

1. **`/series/{slug}` direct-access gap:** `SeriesNavigationPolicy` is not enforced in the slug page read query — a user could bypass UI by typing a disallowed URL directly. Should be enforced in the handler or repository (return 404 or "not available" for ineligible statuses).
2. **Data cleanup:** Inconsistent `PROPOSAL_DRAFT` series with `UNDER_EDITORIAL_REVIEW` proposal rows — stale/test data that should be reconciled.
3. **Cover crop-upload pipeline:** Implement crop-to-`1000×1500` before upload; no DB schema change needed.
