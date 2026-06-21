# Editor: Proposal Review Cover Layout + Status/Query Correctness Fix

**Date:** 2026-06-19
**Branch:** `feature/Mangaka`

## Issues Fixed

### Issue 1 — Cover image layout broken
- Cover was in the right sidebar (`md="4"`) as a bare `<MudImage>` with no size constraints, causing overflow at full natural resolution.
- **Fix:** Moved cover to a new left column (`md="3"`), matching the `/series/{slug}` page design:
  - Container: `max-width: 220px`, neutral `#f1f5f9` background, `border-radius: 12px`
  - Image: `width: 100%; max-height: 310px; object-fit: cover`
  - Placeholder: `MudIcon` (`MenuBook`, 72px) when cover URL is null, matching `SeriesPage.razor`
- Layout is now responsive 3-column (desktop: cover `md="3"` | info `md="6"` | sidebar `md="3"`; mobile: stack vertically)

### Issue 2 — Proposal status/query logic incorrect
- `EditorProposalDetailDto` had only `StatusCode` (mapped to `SeriesProposal.StatusCode`) — no `SeriesStatusCode` exposed.
- Handler eligibility checked only `proposal.StatusCode == "UNDER_EDITORIAL_REVIEW"`, ignoring `Series.StatusCode`. A stale proposal row on a `PROPOSAL_DRAFT` series appeared actionable.
- `EditorSeriesDto` had no proposal fields — Series Library always showed "Review" and navigated to a non-existent `/editor/series/{id}` route.
- **Fix:**
  - Added `ProposalStatusCode` + `SeriesStatusCode` to `EditorProposalDetailDto` (old `StatusCode` removed, replaced by two explicit fields)
  - Handler `eligible` now requires both `Series.StatusCode == "UNDER_EDITORIAL_REVIEW"` AND `Proposal.StatusCode == "UNDER_EDITORIAL_REVIEW"`
  - Added `LatestProposalId` and `LatestProposalStatusCode` to `EditorSeriesDto`
  - Series Library now has `CanReview()` helper: `Review` → `/editor/proposals/{id}` when actionable; `View` → proposal detail or `/series/{slug}` otherwise
  - UI shows a warning alert when series and proposal statuses differ

## Files Changed

| File | Change |
|------|--------|
| `Domain/Interfaces/ISeriesProposalRepository.cs` | Added `GetLatestForSeriesBatchAsync` |
| `Infrastructure/Repositories/SeriesProposalRepository.cs` | Implemented batch query (ordered, client-grouped in handler); fixed CS8602 on ThenInclude with `!` |
| `Application/DTOs/Manga/SeriesProposalDtos.cs` | `StatusCode` → `ProposalStatusCode` + new `SeriesStatusCode` |
| `Application/DTOs/Editor/EditorSeriesDtos.cs` | Added `LatestProposalId`, `LatestProposalStatusCode` |
| `Application/Features/Editor/SeriesProposals/Queries/GetEditorProposalDetail/GetEditorProposalDetailQueryHandler.cs` | Maps `SeriesStatusCode`; cross-checks series status in eligibility |
| `Application/Features/Editor/Series/Queries/GetEditorSeries/GetEditorSeriesQueryHandler.cs` | Injects `ISeriesProposalRepository`; enriches DTOs with latest proposal data |
| `Web/Components/Pages/Editor/ProposalReviewDetail.razor` | 3-column responsive layout; sized cover with placeholder; shows series/proposal status |
| `Web/Components/Pages/Editor/SeriesList.razor` | Conditional Review/View routing by combined status check |

## Build Result
- `dotnet clean && dotnet build --no-incremental`: **60 warnings, 0 errors**
- Zero warnings from changed files (verified via `Select-String` filter)
- Baseline warnings unchanged (all pre-existing)

## Manual Test Checklist
1. Proposal review detail with cover: cover appears left, properly constrained (`max-width: 220px`, `max-height: 310px`, `object-fit: cover`)
2. Cover does not overflow page height or width
3. Submission panel remains readable on the right
4. Claim/action panel remains readable on the right
5. Missing cover shows placeholder icon (not raw broken image)
6. Proposal with series at `PROPOSAL_DRAFT` but old `UNDER_EDITORIAL_REVIEW` proposal: UI does not show actionable controls
7. Series Library: `Review` shown only when `Series.StatusCode == UNDER_EDITORIAL_REVIEW` AND `LatestProposalStatusCode == UNDER_EDITORIAL_REVIEW`
8. Series Library: `View` routes to `/editor/proposals/{id}` when non-actionable proposal exists; `/series/{slug}` when none
9. Proposal detail hides claim/review actions when series status is not `UNDER_EDITORIAL_REVIEW`
10. Warning alert shown when series and proposal statuses differ

## Remaining Tasks
- None for this session
