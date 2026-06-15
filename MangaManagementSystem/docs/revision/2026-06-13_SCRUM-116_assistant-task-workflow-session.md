# SCRUM-116 Revision Note — Assistant Task Workflow Session

**Ticket:** SCRUM-116  
**Title:** Stream D — [Assistant] Task Workflow  
**Branch:** `feature/scrum-116-assistant-task-workflow-local`  
**Date:** 2026-06-13  
**Author:** Kiro AI Assistant  

> **⚠️ Warning:** This work is local only. Do not push to remote until all commits are verified and tested.

---

## Current Status & Next Step

| Item | Status |
|------|--------|
| Phase 1 (Frontend integration) | ✅ Complete |
| Phase 2A (Backend DTOs & contracts) | ✅ Complete |
| Phase 2B (Infrastructure & SQL) | ✅ Complete |
| Phase 3 (Architecture correction) | ✅ Complete |
| Phase 4 (File upload UI) | ⏳ Pending |

**Next exact step:** Test deployed API endpoint and Web UI integration. Verify `POST /api/assistant/tasks/{taskId}/submit-work` with multipart/form-data. Finalize after API/UX validation.

> **Architecture correction:** Web pages now call API endpoints via typed clients instead of Application/Infrastructure services directly.

---

## Completed Commits

| Hash | Message | Purpose |
|------|---------|---------|
| `356522f` | add assistant task API client and register in DI | API client infrastructure (`IAssistantTaskApiClient`, `AssistantTaskApiClient`) and DI registration |
| `ea207d7` | add assistant task submission API controller | `AssistantTaskController` with `/submit-work` endpoint, file upload orchestration |
| `ee560a2` | Register assistant task submission service | DI registration for `IAssistantTaskSubmissionService` → `AssistantTaskSubmissionService` |
| `79b7f56` | Implement assistant task submission infrastructure service | `AssistantTaskSubmissionService` implementation using ADO.NET to call SQL stored procedure |
| `636164f` | Add assistant task submission contract | `IAssistantTaskSubmissionService` interface + DTOs (`AssistantTaskSubmitRequestDto`, `AssistantTaskSubmitResultDto`) |
| `3d4e4bc` | Add assistant task submit stored procedure | `manga.usp_AssistantTask_SubmitWork` — owns transaction and state transitions |
| `c668e0c` | Add SCRUM-116 phase 1 revision note | Documents frontend-only integration |
| `cde8a15` | Integrate assistant task submission | Integrates submit button with `UpdateChapterPageTaskAsync()` for status-only submission |
| `6ed7ec8` | Integrate assistant task detail and workspace | Loads real task data in TaskDetail & StudioWorkspace |

---

## Files Changed So Far

### SQL
1. `src/MangaManagementSystem.SqlServer/Stored Procedures/manga.usp_AssistantTask_SubmitWork.sql`

### Application
1. `src/MangaManagementSystem.Application/Contracts/IAssistantTaskSubmissionService.cs`
2. `src/MangaManagementSystem.Application/DTOs/AssistantTaskSubmitRequestDto.cs`
3. `src/MangaManagementSystem.Application/DTOs/AssistantTaskSubmitResultDto.cs`

### Infrastructure
1. `src/MangaManagementSystem.Infrastructure/Services/AssistantTaskSubmissionService.cs`

### API (New)
1. `src/MangaManagementSystem.API/Controllers/Assistant/AssistantTaskController.cs` (new)
2. `src/MangaManagementSystem.API/Program.cs` (updated with AddInfrastructure)

### Web/Razor
1. `src/MangaManagementSystem.Web/Components/Pages/Assistant/TaskDetail.razor`
2. `src/MangaManagementSystem.Web/Components/Pages/Assistant/StudioWorkspace.razor`
3. `src/MangaManagementSystem.Web/Components/Pages/Assistant/SubmitTaskWork.razor` (updated to use API client)
4. `src/MangaManagementSystem.Web/Services/Api/IAssistantTaskApiClient.cs` (new)
5. `src/MangaManagementSystem.Web/Services/Api/AssistantTaskApiClient.cs` (new)

### Docs
1. `docs/revision/2026-06-13_SCRUM-116_assistant-task-workflow-phase1.md` (created)

---

## Business Rules Preserved

1. Assistant moves task from **ASSIGNED → UNDER_REVIEW** (only).
2. Assistant **never** sets status to **COMPLETED**.
3. **COMPLETED** is exclusively an Editor/review outcome.
4. **Rework** is initiated by Editor only (Editor → Assistant: COMPLETED → UNDER_REVIEW → ASSIGNED).
5. Assistant should **not** have "Request Rework" capability.
6. Full submit should eventually link `CompletedPageVersionId` via file upload.

---

## Architecture Decisions

| Layer | Responsibility |
|-------|----------------|
| **SQL** | `manga.usp_AssistantTask_SubmitWork` owns transaction, state transitions, and `CompletedPageVersionId` linkage. |
| **API Controller** | `AssistantTaskController.SubmitWorkAsync()` receives multipart/form-data, uploads file to Cloudinary, calls Application service. |
| **Application** | `IAssistantTaskSubmissionService.SubmitTaskWorkAsync()` orchestrates submission DTO construction and calls Infrastructure. |
| **Infrastructure** | `AssistantTaskSubmissionService` executes stored procedure with ADO.NET/SqlClient. |
| **Web API Client** | `AssistantTaskApiClient` wraps HTTP calls with `MultipartFormDataContent` for IBrowserFile uploads. |
| **Web/Razor** | `SubmitTaskWork.razor` uses `InputFile`, `IBrowserFile`, and `AssistantTaskApiClient` for submission. |

---

## Current Pending Work

1. **Build verification** — ✅ Completed (API and Web projects build successfully).
2. **Deploy and test API endpoint** — Ensure `POST /api/assistant/tasks/{taskId}/submit-work` works with IIS Express/Kestrel.
3. **Deploy and test Web UI** — Verify `SubmitTaskWork.razor` file upload flow through API.
4. **Create final revision note** after Phase 4 (full file upload) completion.
5. **Clean old stash entries** — only after confirming all work is committed and pushed.

---

## Known Warnings/Risks

| Issue | Details |
|-------|---------|
| **NuGet warnings** | `MailKit/MimeKit` warnings may appear if base branch does not include the package fix commit. |
| **SQL syntax** | Stored procedure syntax is not validated by `dotnet build`. Test in SSMS/SQL Server before deployment. |
| **Partial failure** | If Cloudinary upload succeeds but SQL submit fails, cleanup is *not* implemented unless an existing `IFileStorageService.DeleteAsync()` method is available. |
| **API base address** | Web client uses hardcoded `https://localhost:7264` as base address. Adjust for deployment. |
| **Transaction scope** | SQL stored procedure owns the transaction; C# side cannot roll back Cloudinary upload. Plan error handling carefully. |

---

## Next Prompt Suggestion for Future AI Session

> "Continue SCRUM-116 Phase 4: implement full file upload flow in SubmitTaskWork.razor after API endpoint is deployed and tested. Verify API endpoint POST /api/assistant/tasks/{taskId}/submit-work works correctly. Test multipart/form-data upload with IBrowserFile. Update API client if needed. Do not edit SQL, DTOs, or Infrastructure unless explicitly asked."

---

## Git Status Summary

```
On branch feature/scrum-116-assistant-task-workflow-local
Changes to be committed:
  - src/MangaManagementSystem.API/Controllers/Assistant/AssistantTaskController.cs
  - src/MangaManagementSystem.API/Program.cs
  - src/MangaManagementSystem.Web/Services/Api/IAssistantTaskApiClient.cs
  - src/MangaManagementSystem.Web/Services/Api/AssistantTaskApiClient.cs
  - src/MangaManagementSystem.Web/Program.cs

Changes not staged:
  - src/MangaManagementSystem.Web/Components/Pages/Assistant/SubmitTaskWork.razor (modified)

Untracked files:
  docs/revision/ (this file)
```

> Note: Most changes are committed. `SubmitTaskWork.razor` requires staging and commit.

---

*End of session note.*
