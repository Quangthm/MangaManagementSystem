# Mangaka Chapter Review History — 2026-06-29

## Branch
`feature/Mangaka`

## Problem
Mangaka chapter cards only showed latest review summary. The Mangaka could not expand a chapter to see previous editorial review decisions, comments, reviewed-by/time, or attached markup files.

## Scope
Added expandable Editorial Review History to `/mangaka/chapters`.

## Reuse / Refactor Decision
Reused the existing `EditorChapterReviewHistoryDto` directly in the Mangaka chapter list DTO rather than creating a second history shape. This avoided duplicating review-history field definitions and kept markup file display fields aligned with the Editor detail page. The Editor display logic was not extracted into a shared component because that would have been a broader UI refactor; instead, the Mangaka page locally mirrors the same display pattern (decision chip, reviewed by, reviewed at, comments, markup file button) with the same safe file-link behavior.

## Files Changed

### Application
- `src/MangaManagementSystem.Application/DTOs/Manga/MangakaChapterDtos.cs`
  - Added `EditorialReviewHistory` to `MangakaChapterListItemDto`
  - Added `MarkupFileName` to `ChapterEditorialReviewSummaryDto`

### Infrastructure
- `src/MangaManagementSystem.Infrastructure/Repositories/MangakaChapterRepository.cs`
  - Reused the chapter review query to load all `ChapterEditorialReview` rows newest-first
  - Mapped them to `EditorChapterReviewHistoryDto`
  - Kept latest review summary for existing card display

### Web
- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/Chapters.razor`
  - Added per-chapter expand/collapse state
  - Added pagination state for review history
  - Added `Review History` toggle button per card
  - Added expandable `Editorial Review History` panel
  - Added max 3 history items per page with Previous/Next controls
  - Added local helpers for decision chip color/label and history pagination

## Backend Behavior
- Query strategy: included full editorial review history in the existing `/mangaka/chapters` list response instead of adding a lazy-load endpoint.
- Authorization: unchanged from existing Mangaka chapter authorization — only chapters returned by the Mangaka chapter repository are visible, and review history is loaded only for those accessible chapters.
- Sorting: review history is ordered newest-first by `ReviewedAtUtc DESC`.
- Markup file handling: review history includes markup file metadata and secure URL if present. The UI uses file name or `Open markup` as visible text and never renders the raw Cloudinary URL as text.

## UI Behavior
- Each chapter card keeps the existing latest review summary.
- If the chapter has review history, a `Review History` toggle button appears on the card.
- Expanding the panel shows `Editorial Review History` beneath the existing card content.
- Max 3 review items are shown per page.
- Previous/Next pagination appears when there are more than 3 history records.
- Each history item shows:
  - decision chip
  - reviewed by
  - reviewed at
  - comments
  - markup file button if attached
- Action buttons (`Edit`, `Submit for Review`, `Schedule Release`) keep their existing handlers and continue to work normally.

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
- [ ] Confirm chapter cards still show latest review summary
- [ ] Click a chapter card/header with review history
- [ ] Confirm Editorial Review History expands
- [ ] Confirm max 3 review records are shown per page
- [ ] Confirm pagination appears when more than 3 reviews exist
- [ ] Confirm reviews are newest-first
- [ ] Confirm decision, comments, reviewed by, reviewed at are shown
- [ ] Confirm markup file button appears when review has attached markup
- [ ] Confirm raw Cloudinary URL is not visible as text
- [ ] Confirm Edit/Submit/Schedule buttons still work and do not accidentally toggle the panel
- [ ] Confirm Mangaka cannot see review history for another Mangaka’s chapter
