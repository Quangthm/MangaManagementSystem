# SCRUM-116 Revision Note — Assistant Task Workflow Session

**Ticket:** SCRUM-116  
**Title:** Stream D — [Assistant] Task Workflow  
**Branch:** `feature/scrum-116-assistant-task-workflow-local`  
**Date:** 2026-06-13  
**Author:** Kiro AI Assistant  

> **⚠️ Warning:** This work is local only. Do not push to remote until Phase 3 is complete and verified.

---

## Current Status & Next Step

| Item | Status |
|------|--------|
| Phase 1 (Frontend integration) | ✅ Complete |
| Phase 2A (Backend DTOs & contracts) | ✅ Complete |
| Phase 2B (Infrastructure & SQL) | ✅ Complete |
| Phase 2C (DI Registration) | ✅ Committed (ee560a2) |
| Phase 3 (File upload UI) | ⏳ Pending |

**Next exact step:** Implement `SubmitTaskWork.razor` file upload UI using `InputFile`, `IBrowserFile`, `IFileStorageService`, and `IAssistantTaskSubmissionService.SubmitTaskWorkAsync()`.

---

## Completed Commits

| Hash | Message | Purpose |
|------|---------|---------|
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

### Web/Razor (No changes in Phase 2 — only Phase 1 modified)
1. `src/MangaManagementSystem.Web/Components/Pages/Assistant/TaskDetail.razor`
2. `src/MangaManagementSystem.Web/Components/Pages/Assistant/StudioWorkspace.razor`
3. `src/MangaManagementSystem.Web/Components/Pages/Assistant/SubmitTaskWork.razor`

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
| **C# (Infrastructure)** | Uploads file to Cloudinary *first*, *then* calls SQL stored procedure. If Cloudinary succeeds but SQL fails, cleanup is not yet implemented. |
| **Application** | Contains only DTOs (`AssistantTaskSubmitRequestDto`, `AssistantTaskSubmitResultDto`) and interface contract (`IAssistantTaskSubmissionService`). No business logic. |
| **Infrastructure** | Uses ADO.NET/SqlClient to execute stored procedure with transaction scope. |
| **Web/Razor** | `SubmitTaskWork.razor` will handle `InputFile`, `IBrowserFile`, and orchestrate upload → submission pipeline. |

---

## Current Pending Work

1. **Commit DI registration** — already committed (`ee560a2`), but verify no uncommitted changes exist.
2. **Build verification** — not run per instructions (no `dotnet build`).
3. **Implement `SubmitTaskWork.razor` file upload UI** — Phase 3:
   - Add `InputFile` component
   - Handle `IBrowserFile` selection
   - Call `IFileStorageService.UploadFileAsync()` for Cloudinary
   - Call `IAssistantTaskSubmissionService.SubmitTaskWorkAsync()` with Cloudinary metadata
   - Handle success/failure states with user feedback
4. **Create final revision note** after Phase 3 completion.
5. **Clean old stash entries** — only after confirming all work is committed.

---

## Known Warnings/Risks

| Issue | Details |
|-------|---------|
| **NuGet warnings** | `MailKit/MimeKit` warnings may appear if base branch does not include the package fix commit. |
| **SQL syntax** | Stored procedure syntax is not validated by `dotnet build`. Test in SSMS/SQL Server before deployment. |
| **Partial failure** | If Cloudinary upload succeeds but SQL submit fails, cleanup is *not* implemented unless an existing `IFileStorageService.DeleteAsync()` method is available. |
| **Transaction scope** | SQL stored procedure owns the transaction; C# side cannot roll back Cloudinary upload. Plan error handling carefully. |

---

## Next Prompt Suggestion for Future AI Session

> "Continue SCRUM-116 Phase 3: implement SubmitTaskWork.razor file upload UI using InputFile/IBrowserFile, IFileStorageService, and IAssistantTaskSubmissionService. Inspect current git status first. Do not edit backend, SQL, DTOs, services, appsettings, packages, or docs unless explicitly asked."

---

## Git Status Summary

```
On branch feature/scrum-116-assistant-task-workflow-local
Untracked files:
  docs/revision/
```

> Note: `docs/revision/` folder is newly created (not committed). This file is untracked.

---

*End of session note.*
