# Current Session — Editor Review Scheduling Placeholder

**Branch:** `feature/Mangaka`
**Date:** 2026-07-04
**Status:** DONE

## Goal
Add Editor-side scheduling visibility/options to the Chapter Review page. Update the Chapter Info panel to show publication scheduling metadata, and add a Publication Schedule placeholder card.

## Architecture Flow
```
Blazor Web -> typed API client -> API Controller -> IMediator.Send (query) -> Application handler -> Infrastructure EF Core / UoW -> SQL Server
```

## Files Changed (5 files)

| Layer | File | Change |
|---|---|---|
| Domain | `Domain/Interfaces/IEditorChapterReviewRepository.cs` | Added `PlannedReleaseDate`, `ReleasedAtUtc`, `UpdatedAtUtc` to `EditorChapterReviewDetail` record |
| Infrastructure | `Infrastructure/Repositories/EditorChapterReviewRepository.cs` | Mapped `chapter.PlannedReleaseDate`, `chapter.ReleasedAtUtc`, `chapter.UpdatedAtUtc` into the read model |
| Application | `Application/DTOs/Editor/EditorChapterReviewDtos.cs` | Added `PlannedReleaseDate`, `ReleasedAtUtc`, `UpdatedAtUtc` to `EditorChapterReviewDetailDto` |
| Application | `Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewDetail/GetEditorChapterReviewDetailQueryHandler.cs` | Passed new fields through from repository to DTO |
| Web | `Web/Components/Pages/Editor/ChapterReviewDetail.razor` | Updated Chapter Info panel + added Publication Schedule placeholder card |

## Data Fields Added
- `PlannedReleaseDate` (DateTime?) — displayed as formatted date or "Not scheduled"
- `ReleasedAtUtc` (DateTime?) — displayed as formatted datetime or "Not released"
- `UpdatedAtUtc` (DateTime?) — displayed as formatted datetime or "—"

## UI Behavior
1. **Chapter Info panel**: Shows Planned Release Date, Released At, Last Updated below existing fields with a divider
2. **Publication Schedule card**: Appears for all statuses EXCEPT CANCELLED and RELEASED
   - If planned date exists: shows "Planned release: <date>"
   - If no planned date: shows "No planned release date has been set."
   - Button "Schedule Release" is disabled with caption about upcoming shared calendar
3. For CANCELLED or RELEASED: the card is completely hidden

## Build Result
**SUCCESS** — 0 errors, pre-existing warnings only (none from changed files)

## Manual Smoke Checklist
- [ ] Open Editor Chapter Review page for a normal non-cancelled/non-released chapter
- [ ] Confirm Chapter Info shows Planned Release Date, Released At, and Last Updated
- [ ] Confirm Publication Schedule card appears
- [ ] Confirm "Schedule Release" button is disabled (no click behavior)
- [ ] Open/review a CANCELLED chapter and confirm scheduling card is hidden
- [ ] Open/review a RELEASED chapter and confirm scheduling card is hidden

## Remaining Follow-ups
- Shared schedule calendar page (next prompt)
- Wire "Schedule Release" button to navigate to shared calendar
- Reschedule and ON_HOLD actions from the Publication Schedule card

## Resume prompt for next AI agent
```
Continue from docs/revision/PublicationScheduling/2026-07-04-editor-review-scheduling-placeholder.md.
The scheduling read-model fields are surfaced but the button is disabled.
Next: implement the shared publication calendar page and wire this button to navigate there.
Branch: feature/Mangaka.
```
