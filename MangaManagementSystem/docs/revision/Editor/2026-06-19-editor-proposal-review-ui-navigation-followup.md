# Editor: Proposal Review UI/Navigation Follow-up

**Date:** 2026-06-19
**Branch:** `feature/Mangaka`

## Issues Fixed

### Issue 1 — Wrong "Claim it" message when series differs
- **Root cause:** The `else if` fallback checked only `ProposalStatusCode == "UNDER_EDITORIAL_REVIEW"`, not `SeriesStatusCode`. When `CanClaim = false` (because series was `PROPOSAL_DRAFT`), the fallback still fired showing the misleading "Claim it" prompt.
- **Fix:** Added a prior `else if` check for `SeriesStatusCode != ProposalStatusCode` that shows a status-mismatch warning instead. The original "Claim it" block now also requires `SeriesStatusCode == "UNDER_EDITORIAL_REVIEW"`.

### Issue 2 — Cover scaling
- **Before:** `object-fit: cover` with `max-height: 310px` cropped artwork.
- **After:** Container `max-width: 240px; min-height: 340px;` with `object-fit: contain`. Full image visible inside a portrait box.

### Issue 3 — Synopsis expansion
- Added "View more / Show less" toggle to both `ProposalReviewDetail.razor` and `SeriesPage.razor`.
- Collapsed at `max-height: 140px` when length > 300 chars.

### Issue 4 — Reusable safe returnUrl navigation
- Created `Web/Services/SafeReturnUrl.cs` — strict validation: must start with `/`, no `//`, no `://`, no `\`, no `javascript:`, only `/editor/`, `/series/`, or exact paths.
- Added `returnUrl` query param support to `ProposalReviewDetail.razor`, `SeriesPage.razor`.
- Updated `CreatorWorkspace.razor` to delegate to shared helper (replaced private `IsSafeLocalReturnUrl`).
- `SeriesList.razor` now passes `?returnUrl=%2Feditor%2Fseries` on all navigation links.
- Fallbacks preserved: `/editor/proposals` for proposal detail, `/mangaka` for series page.

## Files Changed

| File | Change |
|------|--------|
| `Web/Services/SafeReturnUrl.cs` | **Created** — shared safe return URL validator |
| `Web/Components/Pages/Editor/ProposalReviewDetail.razor` | Cover scaling (`object-fit: contain`), synopsis toggle, message cascade fix, returnUrl support |
| `Web/Components/Pages/Editor/SeriesList.razor` | `returnUrl` query params on all navigation links |
| `Web/Components/Pages/Series/SeriesPage.razor` | Synopsis toggle, returnUrl support |
| `Web/Components/Pages/Mangaka/CreatorWorkspace.razor` | Refactored to use shared `SafeReturnUrl`, added `@using` |

## Build Result
- `dotnet clean && dotnet build --no-incremental`: **60 warnings, 0 errors**
- Zero new warnings from changed files
- All matched warnings are pre-existing MUD0002 from CreatorWorkspace

## Manual Test Checklist
1. Proposal with mismatched statuses: "Current series state differs" message shown, not "Claim it"
2. Proposal with matching statuses and unclaimed: "Claim it" message still shown correctly
3. Cover image: fully visible in portrait box without cropping
4. Missing cover: placeholder icon shown
5. Synopsis > 300 chars: collapsed at 140px, "View more" expands, "Show less" collapses
6. Synopsis ≤ 300 chars: displayed inline without toggle
7. SeriesPage long synopsis: same View more/Show less behavior
8. Series Library "View Series": navigates to `/series/{slug}?returnUrl=%2Feditor%2Fseries`
9. SeriesPage back with safe returnUrl: returns to `/editor/series`
10. SeriesPage back without returnUrl: falls back to `/mangaka`
11. Proposal Detail back with safe returnUrl: returns to `/editor/series`
12. Proposal Detail back without returnUrl: falls back to `/editor/proposals`
13. Unsafe returnUrl values (e.g. `https://evil.com`, `javascript:alert(1)`): ignored, fallback used
14. Build: 0 errors, 0 changed-file warnings

## Remaining Tasks
- Data cleanup for inconsistent `PROPOSAL_DRAFT` series with `UNDER_EDITORIAL_REVIEW` proposal rows (stale/test data)
