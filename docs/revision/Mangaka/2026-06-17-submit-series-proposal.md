# Session Note — Submit Series Proposal (BF-SERIES-003)

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-17
- **Stored procedure used:** `manga.usp_SeriesProposal_Submit`

---

## Task Summary

Implement BF-SERIES-003 — Submit Series Proposal for Editorial Review. An active Mangaka
contributor can now select an existing `PROPOSAL_DRAFT` series card on their dashboard, upload
a required proposal document (PDF/DOC/DOCX), and formally submit it for editorial review.

This task also introduces the **MediatR/CQRS pattern** for Mangaka workflows. The Submit
Proposal workflow is the first to use `IMediator.Send(command)` in the API controller. The
existing Create Draft endpoint retains the transitional `ISeriesService` path and will be
migrated in a later dedicated task.

---

## Architecture Flow

```
MangakaDashboard.razor
  → IMangakaSeriesApiClient.SubmitProposalAsync  (Web typed client)
  → POST /api/mangaka/series/{seriesId}/proposal-submissions  (multipart/form-data)
  → MangakaSeriesController.SubmitProposalAsync  (thin API controller)
  → IMediator.Send(SubmitSeriesProposalCommand)  (MediatR dispatch)
  → SubmitSeriesProposalCommandHandler  (Application orchestration)
  → IFileStorageService.UploadFileAsync  (Cloudinary + SHA-256)
  → ISeriesProposalRepository.SubmitSeriesProposalViaProcAsync  (Infrastructure SP wrapper)
  → manga.usp_SeriesProposal_Submit  (SQL Server — owns transaction, audit, state transition)
```

Auth boundary: same transitional `X-Actor-User-Id` header pattern as Create Draft.

---

## MediatR/CQRS Files Added

```
src/MangaManagementSystem.Application/Features/Mangaka/SeriesProposals/Commands/SubmitSeriesProposal/
  SubmitSeriesProposalCommand.cs        — IRequest<SeriesProposalSubmittedDto> record
  SubmitSeriesProposalCommandHandler.cs — IRequestHandler; owns validation, upload, SP call, cleanup
```

MediatR 12.4.1 registered in `Application/DependencyInjection.cs`:
```csharp
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
```

The handler assembly scan automatically picks up all future handlers added under `Features/`.

---

## Files Changed by Layer

### Application
- `DependencyInjection.cs` — added `services.AddMediatR(...)` registration.
- `DTOs/Manga/SeriesProposalSubmittedDto.cs` *(new)* — result DTO in the shared DTO folder (not the Features subfolder) so Web/API consume it cleanly. Fields: `SeriesId`, `SeriesProposalId`, `ProposalVersionNo` (short), `SeriesStatusCode`, `ProposalStatusCode`.
- `Features/Mangaka/SeriesProposals/Commands/SubmitSeriesProposal/SubmitSeriesProposalCommand.cs` *(new)* — command record.
- `Features/Mangaka/SeriesProposals/Commands/SubmitSeriesProposal/SubmitSeriesProposalCommandHandler.cs` *(new)* — handler.
- `MangaManagementSystem.Application.csproj` — added `MediatR 12.4.1` PackageReference.

### Domain
- `Interfaces/ISeriesProposalRepository.cs` — added `SubmitSeriesProposalViaProcAsync(...)` method with full parameter contract and XML doc comment.

### Infrastructure
- `Repositories/SeriesProposalRepository.cs` — implemented `SubmitSeriesProposalViaProcAsync` using `CommandType.StoredProcedure` (ADO.NET, consistent with `SeriesRepository`). Strongly typed `SqlParameter`s (GUIDs as `UniqueIdentifier`, `Char(64)` for hash, `BigInt` for size). Output parameters `@series_proposal_id` (UniqueIdentifier) and `@proposal_version_no` (SmallInt) captured correctly. Added `MapSqlException` for known custom error numbers (`57001`–`57004`) and standard SQL constraint codes (`2627`, `2601`, `547`). Raw SQL text never propagated.

### API
- `Contracts/SubmitSeriesProposalForm.cs` *(new)* — `IFormFile? ProposalFile`.
- `Controllers/MangakaSeriesController.cs` — added `IMediator` constructor injection alongside existing `ISeriesService`. Added `POST {seriesId:guid}/proposal-submissions` endpoint (`[Consumes("multipart/form-data")]`). Controller: resolves actor from `X-Actor-User-Id`, validates file present, reads bytes, creates command, calls `_mediator.Send`, returns `200 OK SeriesProposalSubmittedDto`. `InvalidOperationException` → `400 BadRequest ApiErrorResponse`. Unexpected → generic `500 Problem`. `CreateDraftAsync` untouched.

### Web
- `Services/Api/IMangakaSeriesApiClient.cs` — added `SubmitProposalAsync(...)` method signature.
- `Services/Api/MangakaSeriesApiClient.cs` — implemented `SubmitProposalAsync`: multipart form with `ProposalFile` part, `X-Actor-User-Id` header, route `api/mangaka/series/{seriesId}/proposal-submissions`, reuses existing `ExtractErrorMessageAsync` helper. Returns deserialized `SeriesProposalSubmittedDto`. No change to `CreateDraftAsync`.
- `Components/Pages/Mangaka/MangakaDashboard.razor` — added Submit Proposal kebab menu item (visible only when `StatusCode == "PROPOSAL_DRAFT"`). Added Submit Proposal modal with: series title header, warning text, required proposal file upload (`MudFileUpload`, PDF/DOC/DOCX only, max 10 MB). Added state fields (`_submitProposalDialogOpen`, `_submitProposalSeriesId`, `_submitProposalSeriesTitle`, `_proposalFileBytes`, `_proposalFileName`, `_proposalContentType`, `_proposalSubmitting`). Added `OpenSubmitProposal`, `CloseSubmitProposal`, `ClearProposalFile`, `OnProposalFileChanged` (pre-reads bytes immediately), `SubmitProposalAsync`. On success: snackbar with version number, card status updated to `UNDER_EDITORIAL_REVIEW` in-memory, modal closed. On failure: friendly snackbar only.

---

## Stored Procedure Used

`manga.usp_SeriesProposal_Submit`

Confirmed contract used:
```sql
@series_id                UNIQUEIDENTIFIER,
@submitted_by_user_id     UNIQUEIDENTIFIER,
@original_file_name       NVARCHAR(260),
@cloudinary_public_id     NVARCHAR(255),
@cloudinary_secure_url    NVARCHAR(1000),
@content_type             NVARCHAR(100),
@file_size_bytes          BIGINT,
@sha256_hash              CHAR(64),
@series_proposal_id       UNIQUEIDENTIFIER OUTPUT,
@proposal_version_no      SMALLINT OUTPUT
```

The stored procedure internally: locks proposal submission per series, snapshots title/synopsis/genre from `Series`, verifies `status_code = PROPOSAL_DRAFT`, verifies submitter is active Mangaka contributor, generates next `proposal_version_no`, calls `manga.usp_FileResource_Create` for `SERIES_PROPOSAL` file, creates `manga.SeriesProposal`, transitions `manga.Series.status_code` to `UNDER_EDITORIAL_REVIEW`, writes `SERIES_PROPOSAL_SUBMITTED` audit event.

The handler does **not** pass title, synopsis, genre, slug, or status — the SP snapshots and transitions them internally.

---

## API Endpoint Added

```
POST /api/mangaka/series/{seriesId:guid}/proposal-submissions
Content-Type: multipart/form-data
Header: X-Actor-User-Id: <actorUserId>
Body: ProposalFile (required, PDF/DOC/DOCX, max 10 MB)

200 OK → SeriesProposalSubmittedDto
400 BadRequest → ApiErrorResponse { message }
500 Problem → generic safe message
```

---

## Typed Client Method Added

```csharp
Task<SeriesProposalSubmittedDto> SubmitProposalAsync(
    Guid actorUserId,
    Guid seriesId,
    byte[] proposalFileBytes,
    string proposalFileName,
    string proposalContentType,
    CancellationToken cancellationToken = default);
```

---

## UI Behavior Added

- `Submit Proposal` kebab menu item appears only when `series.StatusCode == "PROPOSAL_DRAFT"`.
- Separate Submit Proposal modal (does not modify Create Draft modal).
- Modal shows: series title, warning about locking draft editing, required proposal file upload.
- File validation in UI (fast feedback): extension must be `.pdf`/`.doc`/`.docx`; content type must match; size ≤ 10 MB. Backend validates identically in `SubmitSeriesProposalCommandHandler`.
- `IBrowserFile` bytes are pre-read immediately in `OnProposalFileChanged`; the `IBrowserFile` reference is not stored across re-renders.
- On success: snackbar shows "Proposal submitted (v{N}) — series is now Under Editorial Review.", card status updated to `UNDER_EDITORIAL_REVIEW`, Submit Proposal menu item hidden for that card.
- On failure: friendly snackbar only; raw SQL/stack trace never shown.

---

## Cloudinary Upload and SQL Compensation Behavior

The handler uploads the proposal file to Cloudinary via `IFileStorageService.UploadFileAsync`
before calling the stored procedure. The SP creates the `SERIES_PROPOSAL` `FileResource` itself
inside the SQL transaction.

If the upload returns a null/empty `Sha256Hash`, the handler aborts (best-effort cleanup, safe
error message) before calling SQL. The SP requires `@sha256_hash` non-null.

If the SQL workflow fails after a successful Cloudinary upload, the handler attempts to delete
the uploaded Cloudinary asset via `DeleteFileAsync(publicId, "raw")`. SERIES_PROPOSAL files are
document-only (PDF/DOC/DOCX) → always `"raw"` resource type in Cloudinary. Cleanup is
best-effort only; cleanup failure is logged safely and the original business error is re-thrown.

---

## CQRS Transitional Note

This task introduces MediatR/CQRS for the Submit Proposal workflow only. The existing
Create Draft endpoint still uses the transitional `ISeriesService` path
(`MangakaSeriesController.CreateDraftAsync` → `ISeriesService.CreateSeriesDraftAsync`).
Create Draft should be migrated to a dedicated `CreateSeriesDraftCommand` in a later task.

---

## Build Result

`dotnet build MangaManagementSystem.slnx` — **Build succeeded, 0 errors.**

19 warnings — all pre-existing (MUD0002 MudBlazor analyzer warnings in Admin/Editor pages,
CS0649 in BoardPolls and AssistantDashboard, CS0414 `_loading` in MangakaDashboard).
No new warnings introduced by this task.

---

## Manual Test Result

**Not run by OpenCode.** Manual browser and database testing is required by a developer.

Manual test checklist:
```
1. Start API:  dotnet run --project src\MangaManagementSystem.API\MangaManagementSystem.API.csproj --launch-profile http
2. Start Web:  dotnet run --project src\MangaManagementSystem.Web\MangaManagementSystem.Web.csproj
3. Login as an ACTIVE Mangaka.
4. Create a new draft using the existing Create Draft flow.
5. Confirm the draft card shows PROPOSAL_DRAFT status.
6. Open the kebab menu on that card → confirm "Submit Proposal" appears.
7. Click Submit Proposal → modal opens with warning text.
8. Try clicking Submit without a file → button disabled (file is required).
9. Try uploading an image file (e.g. .png) → friendly warning snackbar, file rejected.
10. Try uploading a file larger than 10 MB → friendly warning snackbar, file rejected.
11. Select a valid PDF/DOCX proposal document → file name shown in modal.
12. Click Submit Proposal → success snackbar with version number.
13. Confirm card status updates to UNDER_EDITORIAL_REVIEW.
14. Confirm "Submit Proposal" no longer appears in the kebab menu for that card.
15. Verify database:
    - Series.status_code = UNDER_EDITORIAL_REVIEW
    - SeriesProposal row created with correct series_id and submitted_by_user_id
    - proposal_version_no = 1 (first submission)
    - FileResource row with file_purpose_code = SERIES_PROPOSAL and sha256_hash populated
16. Confirm the Create Draft modal still has no proposal document upload field.
17. Confirm no raw SQL/stack trace appears in UI or API responses.
18. Confirm API console logs show request received; no secrets exposed.
```

---

## Known Risks / Transitional Debt

1. **Create Draft still uses `ISeriesService` transitional path.** To be migrated to a
   `CreateSeriesDraftCommand`/handler in a later dedicated task. Documented with a comment
   in `MangakaSeriesController.CreateDraftAsync`.

2. **`features.cs` placeholder file** in `Application/Features/` is dead code. Left untouched
   as instructed; remove in a dedicated cleanup task.

3. **`SaveCoverUpload` in dashboard calls `IFileStorageService` and `IFileResourceService`
   directly** (pre-existing tech debt). Not in scope for this task; note for future cleanup.

4. **Dashboard series list loads all series via `ISeriesService.GetAllSeriesAsync()` directly**
   (not through a typed API client). Pre-existing pattern; migration is out of scope here.

5. **Transitional `X-Actor-User-Id` header auth** — still in use. Should be replaced with
   proper JWT/shared-cookie auth when that system is implemented.

6. **Cloudinary cleanup resource type is hardcoded `"raw"`** for SERIES_PROPOSAL. This is
   correct for document-only proposals (PDF/DOC/DOCX). If the proposal file scope ever expands
   to images, this will need revisiting.

7. **`ISeriesProposalRepository` existing methods** (`ClaimEditorialReview`, `RequestRevision`,
   `PassToBoard`, `Cancel`) use `ExecuteSqlRawAsync` string-based SP calls, inconsistent with
   the new `SubmitSeriesProposalViaProcAsync` ADO.NET pattern. Refactoring those is out of scope.

---

## Next Recommended Prompt

> Implement BF-SERIES-002 — Edit Series Draft Profile. This requires:
> - Creating `manga.usp_Series_UpdateProfile` SQL stored procedure (does not exist yet).
> - Adding `UpdateSeriesDraftViaProcAsync` to `ISeriesRepository`.
> - Implementing the SP wrapper in `SeriesRepository`.
> - Adding a `UpdateSeriesDraftCommand`/handler using MediatR (CQRS pattern established here).
> - Adding `PUT /api/mangaka/series/{seriesId}/draft-profile` endpoint.
> - Adding the Edit Draft modal to `MangakaDashboard.razor` (currently missing; the kebab
>   menu "Upload Cover" covers cover only; title/synopsis/genre/language editing needs a modal).
> 
> Separately: migrate `CreateSeriesDraftAsync` from `ISeriesService` to a
> `CreateSeriesDraftCommand`/handler following the CQRS pattern now established.
