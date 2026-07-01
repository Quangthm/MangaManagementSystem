# UI Text Cleanup and Chapter Audit Events — 2026-06-29

## Branch
`feature/Mangaka`

## Problem
Runtime testing showed remaining visible mojibake/punctuation issues in Mangaka proposal UI. Chapter submit/review workflows also needed audit verification so status-changing actions are traceable.

## Scope
Cleaned visible Mangaka proposal UI mojibake and added/verified audit events for:
- Mangaka submit chapter for editorial review
- Editor chapter editorial review decision

## UI Text Cleanup
Fixed remaining visible mojibake in the edit-draft modal slug preview text (line 738 in `MangakaDashboard.razor`). Replaced corrupted UTF-8 mojibake text `â€"` with ASCII-safe wording: "Preview only. The server generates the final slug from the title."

All other remaining mojibake matches in `MangakaDashboard.razor` are in C#/Razor comments only and were left untouched.

## Audit Implementation

### Mangaka submit-for-review
Previously missing. Now inserts an `AuditEvent` in the same EF transaction as the status change.

- **Action code**: `CHAPTER_SUBMITTED_FOR_EDITORIAL_REVIEW`
- **Actor**: submitting Mangaka user id (`actorUserId`)
- **Entity type**: `Chapter`
- **Entity id**: chapter id as string
- **Detail JSON**:
  - `chapter_id`
  - `series_id`
  - `old_status_code` (DRAFT or REVISION_REQUESTED)
  - `new_status_code` (UNDER_REVIEW)
  - `submitted_by_user_id`
  - `submitted_at_utc`
- **Transaction**: inserted in the same EF transaction (`BeginTransactionAsync`/`CommitAsync`) as the chapter status update.

### Editor chapter editorial review decision
Already exists and is complete. Verified in `EditorChapterReviewRepository.cs` (lines 354-380).

- **Action code**: `CHAPTER_EDITORIAL_REVIEW_RECORDED`
- **Actor**: reviewer user id
- **Entity type**: `Chapter`
- **Entity id**: chapter id as string
- **Detail JSON**:
  - `chapter_id`
  - `series_id`
  - `chapter_editorial_review_id`
  - `old_status_code`
  - `new_status_code`
  - `decision_code`
  - `has_markup_file`
  - `markup_file_id`
  - `markup_file_name`
  - `markup_content_type`
  - `markup_file_size`
- **Transaction**: inserted in the same EF transaction as the FileResource (if any), ChapterEditorialReview, and chapter status update.

No changes were needed to the editor review decision audit implementation.

## Stored Procedure Baseline
`manga.usp_ChapterEditorialReview_RecordDecision` was used only as parity reference for audit action codes and detail JSON fields. No stored procedure was added or called.

## Files Changed
- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor` — cleaned remaining user-visible mojibake in slug preview text
- `src/MangaManagementSystem.Infrastructure/Repositories/MangakaChapterRepository.cs` — added `AuditEvent` insertion to `SubmitChapterForReviewAsync`

## Build Result
```text
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded
0 errors
49 warnings
```
Warnings are pre-existing.

## Manual Test Checklist
- [ ] Open Mangaka proposal UI and confirm no visible mojibake in targeted strings.
- [ ] Submit a chapter for editor review.
- [ ] Confirm chapter status becomes `UNDER_REVIEW`.
- [ ] Confirm audit event exists for submit-for-review.
- [ ] Confirm audit detail JSON includes chapter_id, series_id, old_status_code, new_status_code.
- [ ] Record editor review decision APPROVED/REVISION_REQUESTED/CANCELLED.
- [ ] Confirm `ChapterEditorialReview` row is created.
- [ ] Confirm chapter status changes correctly.
- [ ] Confirm audit event exists with `CHAPTER_EDITORIAL_REVIEW_RECORDED`.
- [ ] Confirm audit detail JSON includes review id, decision, old/new status, and markup information when attached.
- [ ] Confirm no stored procedure was added.
