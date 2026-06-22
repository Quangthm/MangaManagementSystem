# Editor Dashboard returnUrl Route Fix

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-19

## Problem

Previous code passed `"/editor/dashboard"` as the returnUrl from the Editor Dashboard (route `/editor`). Since `/editor/dashboard` is not a registered Blazor route, clicking "Back to Dashboard" resulted in a 404.

## Fixes

### 1. EditorDashboard.razor — returnUrl source

Changed `SeriesNavigationUrlBuilder.BuildEditorSeriesOrProposalHref(..., "/editor/dashboard")` to `"...", "/editor"`.

### 2. SafeReturnUrl.cs — accept `/editor`

Added `value == "/editor"` to the accepted paths list. Previously only paths starting with `/editor/` were accepted; the exact `/editor` string was rejected.

### 3. ProposalReviewDetail.razor — label mapping

Updated label mapping to use `/editor` instead of `/editor/dashboard`:

- `/editor` → "Back to Dashboard"
- `/editor/series` → "Back to Series"
- `/editor/proposals` → "Back to Queue"
- any other safe URL → "Back to Queue"

## Files Changed

- `src/MangaManagementSystem.Web/Components/Pages/Editor/EditorDashboard.razor` — returnUrl param from `"/editor/dashboard"` to `"/editor"`
- `src/MangaManagementSystem.Web/Services/SafeReturnUrl.cs` — added `value == "/editor"` exact path
- `src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalReviewDetail.razor` — label key from `"/editor/dashboard"` to `"/editor"`

## Build Result

- **Errors:** 0
- **Warnings:** 60 (all pre-existing)

## Changed-File Warning Check

Command:
```powershell
dotnet build ... --no-incremental 2>&1 |
    Select-String -Pattern "EditorDashboard|ProposalReviewDetail|SeriesNavigationUrlBuilder|SafeReturnUrl"
```

Result: Zero matches — no warnings from changed files.

## Manual Test Checklist

- [ ] Open Editor Dashboard at `/editor`.
- [ ] From Recent Series Activity, open proposal review — URL contains `returnUrl=%2Feditor`.
- [ ] Back label says "Back to Dashboard".
- [ ] Clicking back navigates to `/editor` successfully.
- [ ] From Proposal Queue, open proposal — back returns to `/editor/proposals`.
- [ ] From Series Library, open proposal — back returns to `/editor/series`.
- [ ] Unsafe returnUrl falls back to `/editor/proposals`.
- [ ] Build: 0 errors, no new changed-file warnings.

## Remaining Tasks

1. Cover crop-upload pipeline: implement crop-to-`1000×1500` before upload.
2. Data cleanup: inconsistent `PROPOSAL_DRAFT` series with `UNDER_EDITORIAL_REVIEW` proposal rows.
