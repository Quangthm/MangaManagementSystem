# Editor Review Scheduling Placeholder

**Date:** 2026-07-04
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors, pre-existing warnings only)

---

## Summary

Added Editor-side scheduling visibility to the Chapter Review page as a read-model + UI task. Updated Chapter Info panel to show publication scheduling metadata. Added a Publication Schedule placeholder card with a disabled button. No scheduling action, no shared calendar, no click behavior implemented yet.

---

## Files Changed

| Layer | File | Purpose |
|---|---|---|
| Domain | `Domain/Interfaces/IEditorChapterReviewRepository.cs` | Added `PlannedReleaseDate`, `ReleasedAtUtc`, `UpdatedAtUtc` fields to `EditorChapterReviewDetail` |
| Infrastructure | `Infrastructure/Repositories/EditorChapterReviewRepository.cs` | Mapped new fields from `Chapter` entity into the read model |
| Application | `Application/DTOs/Editor/EditorChapterReviewDtos.cs` | Added `PlannedReleaseDate`, `ReleasedAtUtc`, `UpdatedAtUtc` to `EditorChapterReviewDetailDto` |
| Application | `Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewDetail/GetEditorChapterReviewDetailQueryHandler.cs` | Passed new fields from repository result to DTO |
| Web | `Web/Components/Pages/Editor/ChapterReviewDetail.razor` | Updated Chapter Info panel; added Publication Schedule placeholder card with helper methods |

---

## Data Fields Added

| Field | Type | Display |
|---|---|---|
| `PlannedReleaseDate` | `DateTime?` | Formatted date or "Not scheduled" |
| `ReleasedAtUtc` | `DateTime?` | Formatted datetime or "Not released" |
| `UpdatedAtUtc` | `DateTime?` | Formatted datetime or "—" |

Formatting follows existing project conventions: `"MMM d, yyyy"` for dates, `"MMM d, yyyy HH:mm"` for datetimes.

---

## UI Behavior

### Chapter Info panel
- Shows Planned Release Date, Released At, Last Updated below existing fields
- Separated by a `MudDivider`
- Uses `FormatDateOrPlaceholder` / `FormatDateTimeOrPlaceholder` helpers

### Publication Schedule card
- Appears for all statuses **except** CANCELLED and RELEASED
- If `PlannedReleaseDate` exists: "Planned release: <formatted date>"
- If `PlannedReleaseDate` is null: "No planned release date has been set."
- "Schedule Release" button is **disabled** with caption: "Scheduling will be available through the shared publication calendar in an upcoming update."
- For CANCELLED and RELEASED chapters: card is completely hidden

---

## Status Availability Rule
- Scheduling option shown for: DRAFT, UNDER_REVIEW, REVISION_REQUESTED, APPROVED, SCHEDULED, ON_HOLD
- Scheduling option hidden for: CANCELLED, RELEASED

---

## Build Result
**SUCCESS** — 0 errors, pre-existing warnings only (none from changed files)

---

## Manual Smoke Checklist
- [ ] Open Editor Chapter Review page for a normal non-cancelled/non-released chapter
- [ ] Confirm Chapter Info shows Planned Release Date, Released At, and Last Updated
- [ ] Confirm Publication Schedule card appears with disabled "Schedule Release" button
- [ ] Confirm clicking the button does nothing
- [ ] Open a CANCELLED chapter; confirm scheduling card is hidden
- [ ] Open a RELEASED chapter; confirm scheduling card is hidden

---

## Follow-up
- **Shared schedule calendar page** — next prompt will implement the shared publication calendar and wire the Schedule Release button to navigate to it
- Reschedule / ON_HOLD actions from the Publication Schedule card
- ON_HOLD recovery (deferred from earlier scheduling handoff)
