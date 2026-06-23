# Task Management — Phase 1 Read/List UI Improvements

**Date:** 2026-06-22
**Branch:** `feature/Mangaka`
**Scope:** Phase 1 only — read/list UI improvements. No reassignment, no Quick Select, no batch SP.

---

## Task Summary

Enhanced the `/mangaka/review-submissions` Mangaka task management page with:
- `PageVersionNo` field added to `ChapterPageTaskDto`
- Populated in both `MapToDtoWithFullContext` and `MapToDtoWithAssistantContext` mappers
- Task cards now show: Series title, Chapter number, Page number, Page version number, Assigned display name, Type, Status, Due date, Priority, Compensation, Region count
- Client-side series/chapter/title search
- Client-side task type filter (BACKGROUND, SHADING, EFFECTS, CLEANUP, DIALOGUE, TYPESETTING, REVIEW, OTHER)
- Assistant autocomplete filter from already-loaded task data
- Existing status filter and action buttons (Approve, Return for Rework, Cancel) unchanged

---

## Architecture Path

```
Web: ReviewSubmissions.razor
  → IMangakaTaskApiClient.GetTasksForReviewAsync()
    → GET /api/mangaka/tasks (X-Actor-User-Id header)
      → ChapterPageTaskService.GetTasksForReviewByCreatorAsync()
        → ChapterPageTaskRepository.GetTasksForReviewByCreatorAsync()
          → EF: deep-include query joining PageRegions → ChapterPageVersion → ChapterPage → Chapter → Series
```

No new endpoints. No DB changes. No SP changes. All filters are client-side on the already-loaded task list.

---

## Files Changed

| Layer | File | Change |
|-------|------|--------|
| **Application** | `Application/DTOs/Manga/ChapterPageTaskDtos.cs` | Added `int? PageVersionNo = null` field to `ChapterPageTaskDto` record |
| **Application** | `Application/Services/ChapterPageTaskService.cs` | Populated `PageVersionNo` from `pageVersion?.VersionNo` in both `MapToDtoWithFullContext` and `MapToDtoWithAssistantContext` |
| **Web** | `Components/Pages/Mangaka/ReviewSubmissions.razor` | Full rewrite: added 4-filter bar (series search, task type, assistant, status), enhanced task cards with all context fields, kept all action buttons and dialogs intact |

---

## DB/SP Impact

**None.** No new stored procedures, no migrations, no schema changes.

---

## Behavior Changed

- Task cards show `PageVersionNo` (e.g., "v3") alongside Page number
- Task cards show Series title with menu-book icon, Chapter/Page/Version with article icon
- Task cards show Assigned display name with person icon
- Task cards show Priority level
- Task cards show Compensation amount in currency format
- Task cards show Region count
- Task type displayed as a `MudChip` badge (was plain text)
- Filter bar now has 4 filter fields in a `MudGrid`:
  - **Series search:** free-text `MudTextField` matching `SeriesTitle`, `ChapterNumberLabel`, `ChapterTitle`, or `TaskTitle` (case-insensitive)
  - **Task type filter:** `MudSelect` with clearable option
  - **Assistant filter:** `MudAutocomplete` from distinct `AssignedToDisplayName`/`AssignedUsername` values in loaded tasks
  - **Status filter:** unchanged existing dropdown

---

## Business-Rule Architecture Note

```
C# Application layer is the main business-rule enforcement layer.
SQL stored procedures are responsible for locking/concurrency-sensitive final guards,
insert/update work, transaction consistency, and audit.
Do not move main business rules into SQL just because a new stored procedure is needed later.
```

Phase 1 read model uses EF `AsNoTracking` deep-include queries in Infrastructure, with client-side filtering in the Razor component. Future Phase 2/3 workflows will follow: Web → typed API client → API controller → Application handler → Infrastructure repository → SQL SP.

---

## Verification

### Build

```
dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental
```

Result:
```
Build succeeded.
0 Errors
57 Warnings (all pre-existing, none from changed files)
```

### Route Verified

- `/mangaka/review-submissions` — `[Authorize(Roles = "Mangaka")]`

### Manual Smoke Checklist

- [ ] Navigate to `/mangaka/review-submissions` as Mangaka
- [ ] Verify stat cards show correct counts (Under Review, Assigned, Completed, Cancelled)
- [ ] Verify task cards show all fields: Type chip, Status badge, Title, Series, Chapter/Page/Version, Assigned to, Priority, Due, Compensation, Regions
- [ ] Test series search: type partial series name → cards filter
- [ ] Test task type filter: select "BACKGROUND" → only BACKGROUND tasks shown
- [ ] Test assistant filter: select an assistant name → only their tasks shown
- [ ] Test combined filters: series search + type + assistant + status
- [ ] Verify Approve button appears and works on UNDER_REVIEW tasks
- [ ] Verify Return for Rework dialog opens with reason input
- [ ] Verify Cancel dialog opens with reason input
- [ ] Verify Cancel button appears on ASSIGNED tasks
- [ ] Verify COMPLETED and CANCELLED tasks show no action buttons
- [ ] Verify empty state shown when no tasks match filters

---

## Known Issues

None introduced by this change.

---

## Follow-ups (Phase 2+)

- Wire `usp_ChapterPageTask_AssignToDifferentUser` for reassignment (SP exists, not wired)
- Add Reassign button + dialog to ASSIGNED task cards
- Add Quick Select batch task creation modal
- New SP `usp_ChapterPageTask_CreateBatch`
- New lookup endpoints for series/chapters/pages/assistants
- Consider route rename from `/mangaka/review-submissions` to `/mangaka/tasks`
