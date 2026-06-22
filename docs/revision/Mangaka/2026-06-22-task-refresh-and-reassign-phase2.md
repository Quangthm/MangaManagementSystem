# Task Refresh Fix and Phase 2 Reassignment

## Branch

`feature/Mangaka`

## Date

2026-06-22

## Task summary

Part A: Fixed stale UI after Approve/Return for Rework/Cancel task actions on `/mangaka/review-submissions`. Added `RefreshTasksAfterMutationAsync()` shared helper that clears all dialog/action state and calls `InvokeAsync(StateHasChanged)` after reloading the task list.

Part B: Implemented Phase 2 task reassignment using existing stored procedure `manga.usp_ChapterPageTask_AssignToDifferentUser`. Mangaka can now reassign ASSIGNED or UNDER_REVIEW tasks to a different eligible Assistant contributor on the same series.

## Architecture path

```text
Web: ReviewSubmissions.razor
  -> IMangakaTaskApiClient (typed API client)
    -> MangakaTaskController (thin API controller)
      -> ChapterPageTaskService (Application service, business validation)
        -> ChapterPageTaskRepository (Infrastructure SP wrapper + EF query)
          -> manga.usp_ChapterPageTask_AssignToDifferentUser (existing SP)
          -> manga.vw_ActiveSeriesContributor (existing view, EF read)
```

## Files changed

| Layer | File | Change |
|-------|------|--------|
| **Web** | `Components/Pages/Mangaka/ReviewSubmissions.razor` | Added `RefreshTasksAfterMutationAsync()` helper used by Approve/Return/Cancel; added Reassign button on ASSIGNED and UNDER_REVIEW cards; added Reassign dialog with assistant selector, reason, and optional updated description; added `OpenReassignDialog`, `ConfirmReassign`, and reassign state fields |
| **Web** | `Services/Api/IMangakaTaskApiClient.cs` | Added `GetEligibleAssistantsAsync`, `ReassignTaskAsync` |
| **Web** | `Services/Api/MangakaTaskApiClient.cs` | Implemented `GetEligibleAssistantsAsync`, `ReassignTaskAsync` with proper header/JSON handling |
| **API** | `Controllers/Mangaka/MangakaTaskController.cs` | Added `GET /api/mangaka/tasks/{taskId}/eligible-assistants`, `POST /api/mangaka/tasks/{taskId}/reassign`; added `TryNotifyAssistantByUserIdAsync` helper; added reassignment SQL error codes (58501-58508) to `MapSqlException` |
| **Application** | `Interfaces/IChapterPageTaskService.cs` | Added `ReassignTaskAsync`, `GetEligibleAssistantsForTaskAsync` |
| **Application** | `Services/ChapterPageTaskService.cs` | Implemented `ReassignTaskAsync` with business validation (status, ownership, same-user, reason); implemented `GetEligibleAssistantsForTaskAsync` |
| **Application** | `DTOs/Manga/ChapterPageTaskDtos.cs` | Added `EligibleAssistantDto`, `ReassignChapterPageTaskRequest`, `ReassignChapterPageTaskResult` records |
| **Domain** | `Interfaces/IChapterPageTaskRepository.cs` | Added `AssignToDifferentUserAsync`, `GetEligibleAssistantsForTaskAsync` |
| **Infrastructure** | `Repositories/ChapterPageTaskRepository.cs` | Added `AssignToDifferentUserAsync` SP wrapper calling `manga.usp_ChapterPageTask_AssignToDifferentUser`; added `GetEligibleAssistantsForTaskAsync` EF query on `ActiveSeriesContributors` view joined with `Users` |

## DB/SP impact

**None.** Uses existing stored procedure `manga.usp_ChapterPageTask_AssignToDifferentUser` as-is. No schema changes, no new SPs, no migrations.

## API endpoints added

- `GET /api/mangaka/tasks/{taskId}/eligible-assistants` - Returns eligible assistants for task reassignment (active contributors of same series with Assistant role, excluding current assignee)
- `POST /api/mangaka/tasks/{taskId}/reassign` - Reassigns a task to a different assistant. Request body: `{newAssignedToUserId, reason, updatedTaskDescription?}`. Returns `{oldChapterPageTaskId, newChapterPageTaskId}`.

## Web typed clients added

- `IMangakaTaskApiClient.GetEligibleAssistantsAsync(actorUserId, taskId)` - Calls `GET eligible-assistants`
- `IMangakaTaskApiClient.ReassignTaskAsync(actorUserId, taskId, request)` - Calls `POST reassign`

## Application validation

Before calling SP, `ChapterPageTaskService.ReassignTaskAsync` validates:

- actorUserId is not empty
- taskId is not empty
- newAssignedToUserId is not empty
- reason is required and max 500 characters
- task exists
- actor created the task (ownership check)
- task status is ASSIGNED or UNDER_REVIEW (not COMPLETED/CANCELLED)
- new assistant is not the same as current assistant
- updated task description defaults to current description if not provided

## Infrastructure SP wrapper behavior

- Calls `manga.usp_ChapterPageTask_AssignToDifferentUser` with `CommandType.StoredProcedure`
- Uses strongly typed `SqlParameter` values
- Uses `DBNull.Value` for optional `@updated_task_description`
- Captures `@new_chapter_page_task_id` output parameter
- SP internally: acquires exclusive app lock, cancels old task, creates new ASSIGNED task, copies region links, writes audit event

## SP final guards (in SQL, not changed)

- Lock acquisition (58501)
- Task existence check (58502)
- COMPLETED/CANCELLED rejection (58503)
- Same-user rejection (58504)
- Reason required (58505)
- Active contributor membership validation through `vw_ActiveSeriesContributor` (58508)
- Cancel old task + create new task + copy regions atomically
- Audit event: `CHAPTER_PAGE_TASK_ASSIGNED_TO_DIFFERENT_USER`

## UI behavior changed

### Part A - Targeted refresh

- After Approve/Return/Cancel succeeds, `RefreshTasksAfterMutationAsync()` is called
- Clears all dialog state (return dialog, cancel dialog, reassign dialog, reasons, selected assistant)
- Reloads task list via API
- Calls `InvokeAsync(StateHasChanged)` to guarantee UI re-render
- Stat cards update immediately from reloaded `_tasks`
- Card status/actions update or card disappears based on current filter

### Part B - Reassignment

- Reassign button appears on ASSIGNED and UNDER_REVIEW task cards (Color.Info)
- Reassign button hidden on COMPLETED and CANCELLED cards
- Clicking Reassign opens a dialog showing:
  - Current assignee display
  - MudSelect dropdown of eligible assistants (loaded from API)
  - Reason textarea (required, max 500 chars)
  - Optional updated task description textarea
- Submit calls `ReassignTaskAsync` -> dialog closes -> snackbar success -> task list reloads
- Old task becomes CANCELLED in the list; new ASSIGNED task appears
- Loading indicator shown while eligible assistants are being fetched
- Empty state alert shown if no eligible assistants found

## Build result

```
dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental
Build succeeded.
0 Errors
57 Warnings (all pre-existing baseline; 3 CS8602 warnings originally introduced in GetEligibleAssistantsForTaskAsync were fixed by replacing chained navigation with explicit joins)
```

## Manual smoke checklist

### Part A - refresh

- [ ] Approve UNDER_REVIEW task updates UI without browser refresh
- [ ] Return for Rework updates UI without browser refresh
- [ ] Cancel Task updates UI without browser refresh
- [ ] Stat cards update after each mutation
- [ ] Card action buttons update after each mutation
- [ ] Filters still work after mutation
- [ ] Dialog state is cleared after mutation

### Part B - reassignment

- [ ] Reassign button appears on ASSIGNED task cards
- [ ] Reassign button appears on UNDER_REVIEW task cards
- [ ] Reassign button does not appear on COMPLETED task cards
- [ ] Reassign button does not appear on CANCELLED task cards
- [ ] Reassign dialog opens
- [ ] Current assignee is visible
- [ ] Eligible assistant selector loads valid assistants for the task series
- [ ] Current assigned assistant is excluded from eligible assistant list
- [ ] Cannot submit without selecting a new assistant
- [ ] Cannot submit with empty reason
- [ ] Successful reassignment closes dialog and shows success snackbar
- [ ] Successful reassignment reloads task list
- [ ] Old task becomes CANCELLED or disappears depending on filters
- [ ] New replacement task appears as ASSIGNED if filters include it
- [ ] Invalid reassignment returns user-safe error

### Regression

- [ ] Back to Mangaka Dashboard still works
- [ ] View in Workspace still works
- [ ] Workspace returnUrl back still works
- [ ] Existing Approve / Return / Cancel backend behavior unchanged
- [ ] Existing task filters still work
- [ ] No DB/schema changes

Runtime smoke not run; user must verify manually.

## Known issues

- 3 new CS8602 warnings in `ChapterPageTaskRepository.GetEligibleAssistantsForTaskAsync` from EF navigation chain (same pattern as existing warnings in the same file, not a new code quality issue)
- `GetTaskDetailAsync` in the API controller still loads all tasks and filters (inefficient, pre-existing)

## Follow-ups

- Optimize `GetTaskDetailAsync` controller to use a single-task query instead of loading all tasks
- Add notification to old assistant when their task is cancelled due to reassignment
- Consider showing reassignment history/audit trail on task cards
- Quick Select batch task creation (Phase 3)
- New SP `usp_ChapterPageTask_CreateBatch` (Phase 3)

## Follow-up warning cleanup

### Files changed

- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/ChapterPageTaskRepository.cs`

### Root cause of warnings

The original `GetEligibleAssistantsForTaskAsync` used chained navigation properties in a LINQ `Select`:

```csharp
.Select(r => r.ChapterPageVersion.ChapterPage.Chapter.SeriesId)
```

Each `.` dereference produced a CS8602 "Dereference of a possibly null reference" warning because `ChapterPageVersion`, `ChapterPage`, and `Chapter` are all nullable navigation properties from the compiler's perspective.

### Fix summary

Replaced the chained navigation property projection with an explicit LINQ query-syntax join on `ChapterPageVersion`, `ChapterPage`, and `Chapter` using their FK ID columns (`ChapterPageVersionId`, `ChapterPageId`, `ChapterId`). This also merged the separate `seriesId` and `currentAssignedUserId` queries into a single query returning both values.

### Build result

```
dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental
Build succeeded.
0 Errors
57 Warnings (matches pre-existing baseline exactly)
```

### Changed-file warning result

Zero new warnings from changed files. All 57 warnings are pre-existing.

---

## Resume prompt for next AI agent

On branch `feature/Mangaka`. The task refresh and reassignment features are implemented, build-verified, and warning-clean. The next agent should:
1. Verify runtime behavior by running the app and testing the smoke checklist
2. If assigned: implement Quick Select batch task creation (Phase 3) which requires a new SP `usp_ChapterPageTask_CreateBatch`
3. Do not change the reassignment SP or existing task lifecycle SPs
