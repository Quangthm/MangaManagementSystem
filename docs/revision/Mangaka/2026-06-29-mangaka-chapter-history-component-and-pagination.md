# Mangaka Chapter History Component and Pagination — 2026-06-29

## Branch
`feature/Mangaka`

## Problem
Review history rendering existed in both Editor and Mangaka contexts, and `/mangaka/chapters` did not paginate chapter cards.

## Scope
Extracted reusable editorial review history display and added 10-card pagination to `/mangaka/chapters`.

## Reuse / Refactor Decision
Created `ChapterEditorialReviewHistoryPanel.razor` as a shared Web component for rendering chapter editorial review history.

The component is now used by:
- `Web/Components/Pages/Editor/ChapterReviewDetail.razor`
- `Web/Components/Pages/Mangaka/Chapters.razor`

The component owns its own internal review-history pagination and accepts:
- `Reviews`
- `PageSize`
- `ShowEmptyState`
- `EmptyText`
- `Title`
- `ShowTitle`

This removed duplicate rendering logic for decision chips, reviewed-by/time, comments, and markup file buttons.

## Lazy-load Decision
Review history remains eager-loaded in the existing `/mangaka/chapters` response.

Lazy-load was deferred because it would require new API/client/controller/query paths and is not necessary until chapter volume grows. Current eager-load remains acceptable for typical Mangaka chapter volumes.

## Files Changed

### Web
- `src/MangaManagementSystem.Web/Components/Shared/ChapterEditorialReviewHistoryPanel.razor`
  - New shared component for Editorial Review History display
  - Handles decision chips, reviewed-by/time, comments, markup file button, empty state, and internal pagination

- `src/MangaManagementSystem.Web/Components/Pages/Editor/ChapterReviewDetail.razor`
  - Replaced local Editorial Review History rendering with shared component
  - Removed duplicated review-history decision color helper if no longer needed

- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/Chapters.razor`
  - Replaced local Editorial Review History rendering with shared component
  - Kept chapter expand/collapse behavior
  - Removed local review-history pagination helpers/state because shared component owns history pagination
  - Added chapter-card pagination with max 10 cards per page
  - Added Previous/Next controls and page indicator
  - Reset chapter-card page to 1 when filters/data reload change

## UI Behavior

### Shared history component
`ChapterEditorialReviewHistoryPanel.razor`:
- Shows optional title
- Shows empty state when there is no history
- Shows max `PageSize` review records per page
- Provides Previous/Next controls when there are more records than `PageSize`
- Shows decision chip
- Shows reviewed by
- Shows reviewed at
- Shows comments
- Shows markup file button if attached
- Uses file name or `Open markup` as visible text
- Does not show raw Cloudinary URL as visible text

### Editor detail usage
`/editor/chapters/{chapterId}` still shows Editorial Review History, now through the shared component.

### Mangaka card usage
`/mangaka/chapters` still shows latest review summary on the card. Expanding `Review History` now renders the shared component with `PageSize = 3`.

### Mangaka chapter-card pagination
`/mangaka/chapters` now paginates chapter cards:
- max 10 cards per page
- pagination applies after existing filters
- pagination appears only when there is more than one page
- existing card actions still work

### Filter reset behavior
Chapter-card pagination resets to page 1 when filters or loaded data change.

### Markup file safe link behavior
Markup file buttons use safe display text:
- file name when available
- otherwise `Open markup`

Raw Cloudinary URLs are not rendered as visible text.

## Build Result

```text
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded
0 errors
47 warnings
```

Warnings are pre-existing.

## Manual Test Checklist
- [ ] Open `/mangaka/chapters`
- [ ] Confirm chapter cards still render correctly
- [ ] Confirm max 10 chapter cards are visible per page
- [ ] Confirm pagination appears when more than 10 chapters match filters
- [ ] Confirm series/status filters reset to page 1
- [ ] Confirm Review History expand/collapse still works
- [ ] Confirm history still shows max 3 review items per page
- [ ] Confirm markup file button still uses safe display text
- [ ] Confirm raw Cloudinary URL is not visible as text
- [ ] Confirm Edit/Submit/Schedule buttons still work
- [ ] Open Editor chapter detail and confirm Editorial Review History still renders correctly
