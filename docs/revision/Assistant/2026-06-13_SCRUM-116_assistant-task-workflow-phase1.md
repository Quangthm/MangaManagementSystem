# SCRUM-116 Revision Note — Phase 1

**Ticket:** SCRUM-116  
**Title:** Stream D — [Assistant] Task Workflow  
**Branch:** `feature/scrum-116-assistant-task-workflow-local`  
**Date:** 2026-06-13  
**Author:** Kiro AI Assistant  

---

## Commits

| Hash | Message |
|------|---------|
| `6ed7ec8` | Integrate assistant task detail and workspace |
| `cde8a15` | Integrate assistant task submission |

---

## Summary

Phase 1 implements frontend integration for the Assistant task workflow using the existing `IChapterPageTaskService` backend contract. All three Assistant workflow pages (TaskDetail, StudioWorkspace, SubmitTaskWork) now load real data from the service layer and support status-only submission from ASSIGNED to UNDER_REVIEW.

---

## Files Changed

### Modified (3)

1. `src/MangaManagementSystem.Web/Components/Pages/Assistant/TaskDetail.razor`
2. `src/MangaManagementSystem.Web/Components/Pages/Assistant/StudioWorkspace.razor`
3. `src/MangaManagementSystem.Web/Components/Pages/Assistant/SubmitTaskWork.razor`

### Created (2) — from SCRUM-93 earlier in session

4. `src/MangaManagementSystem.Web/Components/Shared/StatusBadge.razor`
5. `src/MangaManagementSystem.Web/Components/Shared/TimestampDisplay.razor`

---

## What Was Completed

### TaskDetail.razor (Commit 1)
- Injected `IChapterPageTaskService`
- Loads task by `TaskId` (Guid) on initialization
- Displays real metadata: TypeCode, StatusCode, PriorityLevel, DueAtUtc, CompletedPageVersionId
- Added loading, error, and not-found states
- Uses `StatusBadge` and `TimestampDisplay` shared components
- Navigation to Studio Workspace and Submit Work preserved

### StudioWorkspace.razor (Commit 1)
- Injected `IChapterPageTaskService`
- Loads task by `TaskId` (Guid) on initialization
- Replaced hardcoded text with real task data
- Added loading, error, and not-found states
- Canvas area kept as placeholder with clear note about FileResource integration
- Uses `StatusBadge` and `TimestampDisplay` shared components

### SubmitTaskWork.razor (Commit 2)
- Changed `TaskId` parameter from `string?` to `Guid`
- Injected `IChapterPageTaskService` and `ISnackbar`
- Loads task on initialization to validate state
- Submit action builds `UpdateChapterPageTaskDto` from loaded task and sets `StatusCode = "UNDER_REVIEW"`
- Preserves existing `CompletedPageVersionId` and `PageRegionIds`
- Only allows submission when task status is `ASSIGNED`
- On success: shows Snackbar toast and navigates to `/assistant/tasks`
- On failure: shows inline error, does not navigate away
- "Save Draft" button explicitly disabled with note
- Non-functional mock `Submitted` boolean removed

---

## What Was Intentionally Not Included

| Feature | Reason |
|---------|--------|
| File upload (PSD, PNG, JPG, TIFF) | Requires full-stack: IFileStorageService + Cloudinary + FileResource + ChapterPageVersion pipeline |
| CompletedPageVersionId creation/linking | Requires ChapterPageVersion service orchestration |
| Submission notes persistence | Not in current `UpdateChapterPageTaskDto` contract |
| "Mark as Completed" button | Business rule: only Editor/review can set COMPLETED |
| "Request Rework" button | Business rule: rework is an Editor action only |
| Task reassignment | Not an Assistant action |
| Page region visualization on canvas | Requires FileResource/image rendering infrastructure |

---

## Verification Status

| Check | Status |
|-------|--------|
| Build successful (`dotnet build`) | Passed (0 errors, 0 warnings) |
| No backend changes | Confirmed |
| No DTO/service/repository changes | Confirmed |
| No database script changes | Confirmed |
| StatusBadge and TimestampDisplay resolve correctly | Confirmed (RZ10012 warnings resolved) |
| Business rules respected | Confirmed |

---

## Business Rule Notes

1. **ASSIGNED** = Assistant is actively working on the task.
2. **UNDER_REVIEW** = Assistant has submitted; waiting for Editor review.
3. **COMPLETED** = Editor has approved the submission. Assistant cannot set this.
4. **CANCELLED** = Task is cancelled. Not implemented in this phase.
5. **Rework** = Editor returns UNDER_REVIEW → ASSIGNED for the same Assistant. Not an Assistant action.
6. **CompletedPageVersionId** is nullable in the current DTO. Status-only submit is valid per the backend contract, but the full workflow should eventually require a linked file.

---

## Next Steps — Phase 2

### High Priority
1. Implement file upload integration: `IFileStorageService.UploadFileAsync()` → `IFileResourceService.CreateFileResourceAsync()` → `IChapterPageVersionService.CreateChapterPageVersionAsync()` → Update task with `CompletedPageVersionId`
2. Add a dedicated `SubmitTaskWorkAsync` method to `IChapterPageTaskService` that orchestrates the full pipeline atomically

### Medium Priority
3. Add submission notes/comment field to the DTO or a separate submission entity
4. Add page region visualization in StudioWorkspace canvas (requires image rendering)
5. Add ownership validation (verify logged-in user matches `AssignedToUserId`)

### Low Priority
6. Add unit tests for submit workflow
7. Add Cloudinary cleanup on submission failure
8. Consider adding a confirmation dialog before submit

---

## Notes for Future AI/Team Members

### Backend Contract Used
```csharp
// Existing method - no changes needed for status-only submit
Task<ChapterPageTaskDto?> UpdateChapterPageTaskAsync(UpdateChapterPageTaskDto dto);

// DTO structure (all fields required except nullable ones)
record UpdateChapterPageTaskDto(
    Guid ChapterPageTaskId,      // Required
    Guid AssignedToUserId,       // Required - preserve from loaded task
    string TypeCode,             // Required - preserve from loaded task
    string StatusCode,           // Required - set to "UNDER_REVIEW"
    int PriorityLevel,           // Required - preserve from loaded task
    DateTime? DueAtUtc,          // Nullable - preserve from loaded task
    Guid? CompletedPageVersionId,// Nullable - preserve or null
    IReadOnlyList<Guid> PageRegionIds // Required - preserve from loaded task
);
```

### Key Design Decisions
- Status-only submit is allowed because `CompletedPageVersionId` is nullable in the DTO and no database CHECK constraint enforces it.
- The UI clearly labels this as a status-only integration with file upload pending.
- All three pages use the same pattern: load task → display → act.
- Shared components (`StatusBadge`, `TimestampDisplay`) are used consistently across all Assistant pages.
