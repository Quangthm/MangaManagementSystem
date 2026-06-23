# Session Note — Return for Rework to Same Assistant (Repair)

## Branch

`feature/Mangaka`

## Date

2026-06-23

## Task summary

Repaired the existing Return for Rework workflow on `/mangaka/review-submissions`. The workflow already existed end-to-end (Web → API → Application → Infrastructure → SP) but had three gaps:

1. **Missing C# Application validation** — `ChapterPageTaskService.ReturnTaskForReworkAsync` called the SP directly with no pre-validation.
2. **Unsafe SQL error mapping** — Error codes 58403 and 58406 leaked raw SQL messages to the UI snackbar.
3. **UI label inconsistencies** — Dialog title said "Return Task for Rework", field label said "Rework Instructions", no helper text, no assistant name display.

No new endpoints, methods, or SPs were added. Only the existing ones were repaired.

## Architecture path

```text
Web: ReviewSubmissions.razor
  → IMangakaTaskApiClient.ReturnTaskForReworkAsync
    → POST /api/mangaka/tasks/{taskId}/return-for-rework
      → MangakaTaskController.ReturnForReworkAsync
        → IChapterPageTaskService.ReturnTaskForReworkAsync (Application validation + delegate)
          → IChapterPageTaskRepository.ReturnTaskForReworkAsync (Infrastructure SP wrapper)
            → manga.usp_ChapterPageTask_ReturnForRework (SQL Server, existing)
```

## Files changed

| Layer | File | Change |
|-------|------|--------|
| **Web** | `Components/Pages/Mangaka/ReviewSubmissions.razor` | Added `_returnDialogTask` field; updated `OpenReturnDialog` to store task DTO; dialog title "Return for Rework"; shows current assistant display name; label "Updated task instructions" with helper text; cleared `_returnDialogTask` in `RefreshTasksAfterMutationAsync` |
| **API** | `Controllers/Mangaka/MangakaTaskController.cs` | Split 58203/58303/58403 into separate safe messages; added 58406 mapping |
| **Application** | `Services/ChapterPageTaskService.cs` | Added C# validation before calling SP: actor ID not empty, task ID not empty, instructions not empty, task exists, actor created the task (ownership), task is UNDER_REVIEW |

## DB/SP impact

**None.** Uses existing `manga.usp_ChapterPageTask_ReturnForRework` as-is. No schema changes.

## API endpoint confirmed

- `POST /api/mangaka/tasks/{taskId}/return-for-rework` — already existed; no route change

## Application validation added

Before calling SP, `ChapterPageTaskService.ReturnTaskForReworkAsync` now validates:

- `actorUserId != Guid.Empty`
- `taskId != Guid.Empty`
- `string.IsNullOrWhiteSpace(updatedTaskDescription)` → "Updated task instructions are required when returning a task for rework."
- Task exists
- `task.CreatedByUserId == actorUserId` (ownership check)
- `task.StatusCode == "UNDER_REVIEW"`
- SP remains final transactional guard (locks, contributor membership, audit)

## SQL error mapping fixed

| Error code | Before | After |
|---|---|---|
| 58401 | Safe ("lock") | Unchanged |
| 58402 | Safe ("not found") | Unchanged |
| 58403 | Raw `ex.Message` | "Only tasks currently under review can be returned for rework." |
| 58406 | Not mapped (raw `ex.Message`) | "You must be an active contributor of this series to return a task for rework." |

## UI behavior changed

- Dialog title: "Return for Rework" (was "Return Task for Rework")
- Shows current assigned assistant display name
- Field label: "Updated task instructions" (was "Rework Instructions")
- Helper text: "Explain what the Assistant should revise before resubmitting."
- Confirm button: "Return for Rework" (unchanged)
- Button visibility: UNDER_REVIEW only (unchanged)
- Post-success: clears dialog state, reloads tasks, refreshes stats (unchanged)

## Build result

```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded.
0 Errors
45 Warnings (all pre-existing baseline)
```

Zero new warnings from changed files.

## Manual smoke checklist

- [ ] UNDER_REVIEW task shows Return for Rework button
- [ ] ASSIGNED task does not show Return for Rework
- [ ] COMPLETED task does not show Return for Rework
- [ ] CANCELLED task does not show Return for Rework
- [ ] Return dialog opens with title "Return for Rework"
- [ ] Current assigned assistant name is visible in dialog
- [ ] "Updated task instructions" label is shown with helper text
- [ ] Submit disabled when instructions field is empty
- [ ] Confirm calls backend successfully
- [ ] Same task changes from UNDER_REVIEW to ASSIGNED (or disappears if filter)
- [ ] Same assistant remains assigned
- [ ] UI refreshes without browser refresh
- [ ] Different-assistant Reassign still works separately
- [ ] Error shows safe message (not raw SQL text)

Runtime smoke not run; user must verify manually.

## Known issues

- None from this repair.

## Follow-ups

- None required for this repair.
