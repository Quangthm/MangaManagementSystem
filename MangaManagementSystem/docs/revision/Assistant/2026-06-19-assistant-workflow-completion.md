# Assistant Task Workflow Completion Session

**Date:** 2026-06-19  
**Goal:** Complete the missing Assistant task workflow so the full lifecycle is demo-ready: Mangaka assigns task -> Assistant views/submits -> Mangaka reviews -> approve/return/cancel.

---

## Changed Files

### API Layer

| File | Change |
|------|--------|
| `API/Controllers/Assistant/AssistantTaskController.cs` | Removed `[AllowAnonymous]` and `Guid.Empty` fallback. Switched to `X-Actor-User-Id` header pattern matching other controllers. Added `TryResolveActorUserId`. Added `ILogger`. |
| `API/Controllers/Mangaka/MangakaTaskController.cs` | **NEW.** Thin controller for Mangaka task-review workflows: GET tasks, GET detail, POST approve, POST return-for-rework, POST cancel. Includes notification creation for each lifecycle event. |

### Application Layer

| File | Change |
|------|--------|
| `Application/Interfaces/IChapterPageTaskService.cs` | Added `ApproveTaskAsync`, `ReturnTaskForReworkAsync`, `CancelTaskAsync`, `GetTasksForReviewByCreatorAsync`. |
| `Application/Services/ChapterPageTaskService.cs` | Implemented the 4 new methods. Added `MapToDtoWithFullContext` with `CompletedOutputUrl`, `CreatedByDisplayName`, `CreatedAtUtc`, `UpdatedAtUtc`. |
| `Application/DTOs/Manga/ChapterPageTaskDtos.cs` | Added `CompletedOutputUrl`, `CreatedByDisplayName`, `CreatedAtUtc`, `UpdatedAtUtc` optional fields to `ChapterPageTaskDto`. |

### Domain Layer

| File | Change |
|------|--------|
| `Domain/Interfaces/IChapterPageTaskRepository.cs` | Added `CancelTaskAsync`, `MarkTaskCompletedAsync`, `ReturnTaskForReworkAsync`, `GetTasksForReviewByCreatorAsync`. |

### Infrastructure Layer

| File | Change |
|------|--------|
| `Infrastructure/Repositories/ChapterPageTaskRepository.cs` | Implemented 4 new methods calling existing SPs: `usp_ChapterPageTask_Cancel`, `usp_ChapterPageTask_MarkCompleted`, `usp_ChapterPageTask_ReturnForRework`. Added `GetTasksForReviewByCreatorAsync` with deep includes for `CompletedPageVersion.PageFile` and `CreatedByUser`. Enhanced `GetByIdWithFullContextAsync` to include `CreatedByUser` and `CompletedPageVersion.PageFile`. |

### Web Layer

| File | Change |
|------|--------|
| `Web/Services/Api/IAssistantTaskApiClient.cs` | Added `actorUserId` parameter to all methods. |
| `Web/Services/Api/AssistantTaskApiClient.cs` | Sends `X-Actor-User-Id` header on all requests instead of using claims directly. |
| `Web/Services/Api/IMangakaTaskApiClient.cs` | **NEW.** Interface for Mangaka task review API client. |
| `Web/Services/Api/MangakaTaskApiClient.cs` | **NEW.** Implementation sending `X-Actor-User-Id` header for all task actions. |
| `Web/Program.cs` | Registered `IMangakaTaskApiClient` / `MangakaTaskApiClient` HttpClient. |
| `Web/Components/Pages/Assistant/AssistantDashboard.razor` | Uses `AuthenticationStateProvider` for user ID. Stats computed from real data. |
| `Web/Components/Pages/Assistant/AssignedTasks.razor` | Replaced hardcoded stat cards (2, 2, 1, 1) with computed values from API data. Uses `AuthenticationStateProvider`. |
| `Web/Components/Pages/Assistant/TaskDetail.razor` | Uses `AuthenticationStateProvider`. Shows submitted output preview. Disables Submit button when status is not ASSIGNED. Download source image link. |
| `Web/Components/Pages/Assistant/SubmitTaskWork.razor` | Uses `AuthenticationStateProvider`. Source page reference image. Disabled state messaging. Removed fake Save Draft button. |
| `Web/Components/Pages/Assistant/StudioWorkspace.razor` | Uses `AuthenticationStateProvider`. Shows submitted output. Disables Submit when not ASSIGNED. Zoom reset button. Download source link. |
| `Web/Components/Pages/Mangaka/ReviewSubmissions.razor` | **REWRITTEN.** Replaced hardcoded mock data with real API data. Stat cards, status filter, submitted output preview, original page image, Approve/Return/Cancel action buttons with MudDialog modals, snackbar feedback, notification creation. |

---

## Endpoints Added/Changed

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/mangaka/tasks` | Get all tasks created by this Mangaka |
| GET | `/api/mangaka/tasks/{taskId}` | Get task detail for Mangaka review |
| POST | `/api/mangaka/tasks/{taskId}/approve` | Approve/complete task (UNDER_REVIEW -> COMPLETED) |
| POST | `/api/mangaka/tasks/{taskId}/return-for-rework` | Return task for rework (UNDER_REVIEW -> ASSIGNED) |
| POST | `/api/mangaka/tasks/{taskId}/cancel` | Cancel task (ASSIGNED/UNDER_REVIEW -> CANCELLED) |
| GET | `/api/assistant/tasks` | (Changed) Now requires X-Actor-User-Id header |
| GET | `/api/assistant/tasks/{taskId}` | (Changed) Now requires X-Actor-User-Id header |
| POST | `/api/assistant/tasks/{taskId}/submit-work` | (Changed) Now requires X-Actor-User-Id header |

---

## Stored Procedures Wired

| SP | Wired From |
|----|-----------|
| `manga.usp_ChapterPageTask_Cancel` | `ChapterPageTaskRepository.CancelTaskAsync` |
| `manga.usp_ChapterPageTask_MarkCompleted` | `ChapterPageTaskRepository.MarkTaskCompletedAsync` |
| `manga.usp_ChapterPageTask_ReturnForRework` | `ChapterPageTaskRepository.ReturnTaskForReworkAsync` |
| `manga.usp_AssistantTask_SubmitWork` | Already wired (unchanged) |
| `manga.usp_ChapterPageTask_Create` | Already wired (unchanged) |

---

## Status Transitions Implemented

```
ASSIGNED --[Assistant submits work]--> UNDER_REVIEW
UNDER_REVIEW --[Mangaka approves]--> COMPLETED
UNDER_REVIEW --[Mangaka returns for rework]--> ASSIGNED (clears output)
ASSIGNED/UNDER_REVIEW --[Mangaka cancels]--> CANCELLED
```

---

## Validation Rules

- **Authentication:** All endpoints require `X-Actor-User-Id` header (transitional pattern). Empty GUID fallback removed.
- **Assistant submit:** Task must be ASSIGNED, file required, max 10MB, PNG/JPEG/WebP only.
- **Mangaka approve:** Task must be UNDER_REVIEW (enforced by SP).
- **Mangaka return:** Task must be UNDER_REVIEW, reason required (enforced by SP + controller).
- **Mangaka cancel:** Task must be ASSIGNED or UNDER_REVIEW, reason required (enforced by SP + controller).
- **Ownership:** Assistant can only see tasks assigned to them. Mangaka can only review tasks they created.

---

## Notifications

Created for each task lifecycle event:
- `TASK_COMPLETED` -- when Mangaka approves
- `TASK_RETURNED_FOR_REWORK` -- when Mangaka returns for rework
- `TASK_CANCELLED` -- when Mangaka cancels

Notifications are best-effort (non-blocking on failure).

---

## Known Limitations

- `usp_ChapterPageTask_AssignToDifferentUser` SP exists but is not wired yet (reassign task).
- Annotation view for Assistant (US-ASSISTANT-005) not implemented -- requires junction table query.
- Task notification when Mangaka first assigns (NEW_TASK_ASSIGNED) is not wired from the creation flow.
- API authentication is still transitional (`X-Actor-User-Id` header), not JWT.
- No MediatR handlers for task workflows -- uses direct service/repository pattern (consistent with existing task code).

---

## Verification

- `dotnet build MangaManagementSystem.sln` -- **Build succeeded, 0 errors, 53 warnings (all pre-existing).**
- Routes verified in code:
  - `/assistant` -- Dashboard with computed stats
  - `/assistant/tasks` -- Computed stat cards
  - `/assistant/task/{id}` -- Full context with submitted output
  - `/assistant/task/{id}/submit` -- File upload with validation
  - `/assistant/studio/{id}` -- Page viewer with zoom
  - `/mangaka/review-submissions` -- Real data, approve/return/cancel actions

---

## Remaining Next Steps

- Wire `usp_ChapterPageTask_AssignToDifferentUser` for task reassignment.
- Wire annotation junction table query for Assistant annotation view.
- Wire NEW_TASK_ASSIGNED notification from task creation flow.
- Replace `X-Actor-User-Id` header with JWT authentication.
- Consider MediatR migration for task workflows for consistency with Mangaka/Editor patterns.
