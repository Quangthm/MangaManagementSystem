# Proposal Review Return Navigation & Cover Size

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-19

## Problem

### Task A — Back button label always said "Back to Queue"

Even when the user arrived at Proposal Review from the Editor Dashboard (returnUrl=`/editor/dashboard`), the back button label showed "Back to Queue" — misleading. Navigation itself was correct (SafeReturnUrl.Resolve returns `/editor/dashboard`), but the label didn't reflect the destination.

### Task B — Dashboard URL generation verification

Verified `EditorDashboard.razor` → `SeriesNavigationUrlBuilder.BuildEditorSeriesOrProposalHref` generates:
- `/editor/proposals/{proposalId}?returnUrl=%2Feditor%2Fdashboard` for non-slug-eligible series with a latest proposal
- `/series/{slug}?returnUrl=%2Feditor%2Fdashboard` for slug-eligible series

Both include `returnUrl` correctly encoded with `Uri.EscapeDataString`. No double-encoding or missing param.

### Task C — Cover image too small

Proposal Review cover was displayed at `max-width: 240px` — smaller than appropriate for the review page. Needed larger 2:3 display.

## Files Changed

- `src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalReviewDetail.razor`

### Change 1 — Dynamic back button label

Added `_backLabel` field computed from resolved `SafeReturnUrl.Resolve`:
- `"/editor/dashboard"` → `"Back to Dashboard"`
- `"/editor/series"` or `"/editor/proposals"` → `"Back to Queue"`
- any other resolved value → `"Back to Queue"`

Back button is now `@_backLabel` instead of hardcoded "Back to Queue".

### Change 2 — Cover size increase

- Cover column: `md="3"` → `md="4"` (wider grid allocation)
- Inner container: `max-width:240px` → `max-width:320px`
- Center info column: `md="6"` → `md="5"` (rebalanced to keep grid sum = 12)
- Right action column: unchanged at `md="3"`
- Aspect ratio stays `2/3`, `object-fit: cover`

### No Changes To

- `EditorDashboard.razor` — Proposal Queue Review button works correctly without returnUrl (fallback is `/editor/proposals`)
- `SeriesNavigationUrlBuilder.cs` — already correct
- `SafeReturnUrl.cs` — already accepts `/editor/dashboard`

## Build Result

- **Errors:** 0
- **Warnings:** 60 (all pre-existing)

## Changed-File Warning Check

Command:
```powershell
dotnet build ... --no-incremental 2>&1 |
    Select-String -Pattern "ProposalReviewDetail|EditorDashboard|SeriesNavigationUrlBuilder|SafeReturnUrl"
```

Result: Zero matches — no warnings from changed files.

## Manual Test Checklist

- [ ] Open proposal detail from Proposal Review Queue → "Back to Queue" → returns to `/editor/proposals`.
- [ ] Open proposal detail from Editor Dashboard Recent Series Activity → "Back to Dashboard" → returns to `/editor/dashboard`.
- [ ] Open proposal detail from Series Library (`returnUrl=/editor/series`) → "Back to Queue" → returns to `/editor/series`.
- [ ] Unsafe returnUrl is ignored, label shows "Back to Queue", falls back to `/editor/proposals`.
- [ ] Cover display is larger (max-width: 320px, 2:3 aspect ratio).
- [ ] Cover uses `object-fit: cover`, not stretched.
- [ ] Missing-cover placeholder fills the same 320px box.
- [ ] No duplicate caption/title under cover.
- [ ] Build: 0 errors, no new changed-file warnings.

## Remaining Tasks

1. **Cover crop-upload pipeline:** Implement crop-to-`1000×1500` before upload; no DB schema change needed.
2. **Data cleanup:** Inconsistent `PROPOSAL_DRAFT` series with `UNDER_EDITORIAL_REVIEW` proposal rows.
