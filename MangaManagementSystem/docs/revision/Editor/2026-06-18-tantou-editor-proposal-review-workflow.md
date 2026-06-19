# Tantou Editor Proposal Review Workflow

## Branch

`feature/Mangaka`

## Date

2026-06-18

## Task Summary

Implemented the full Tantou Editor series-proposal review workflow end-to-end through the
project's canonical architecture path:

```
Blazor Web -> typed API client -> API controller -> MediatR commands/queries
-> Application handlers -> Infrastructure repository / SP wrappers / EF read queries
-> SQL Server
```

The previous `ProposalList.razor` and `ProposalReviewDetail.razor` pages directly injected
`ISeriesProposalService`, `IFileStorageService`, and `IFileResourceService` from the
Application layer into Razor — a Clean Architecture violation. Both pages were rewritten to
use a new typed `IEditorProposalApiClient` instead. Cloudinary/file-storage access has been
fully removed from the Web project. Markup uploads now flow through the API as
`multipart/form-data`, and the Application handler performs the Cloudinary upload
orchestration (matching the existing `SubmitSeriesProposalCommandHandler` pattern).

## Architecture Path

```
ProposalList.razor / ProposalReviewDetail.razor
  -> IEditorProposalApiClient (typed Web client, X-Actor-User-Id header)
  -> EditorProposalsController (thin, api/editor/proposals)
  -> IMediator.Send(query/command)
  -> Editor proposal query/command handlers (Application layer)
  -> ISeriesProposalRepository
  -> EF AsNoTracking reads for queue/detail
  -> existing stored procedures for writes:
     - manga.usp_SeriesProposal_ClaimEditorialReview
     - manga.usp_SeriesProposal_RequestRevision
     - manga.usp_SeriesProposal_PassToBoard
     - manga.usp_SeriesProposal_CancelEditorialReview
```

No direct Web-to-Application, Web-to-Infrastructure, Web-to-EF, Web-to-Cloudinary, or
Web-to-stored-procedure calls remain in the migrated pages.

## Files Changed

### Modified

| File | Change |
|------|--------|
| `src/MangaManagementSystem.Application/DTOs/Manga/SeriesProposalDtos.cs` | Added `EditorProposalDetailDto` (snapshot + permission flags) and `EditorReviewActionResultDto` |
| `src/MangaManagementSystem.Domain/Interfaces/ISeriesProposalRepository.cs` | Added `IsActiveTantouEditorContributorAsync(Guid seriesId, Guid userId, CancellationToken)` |
| `src/MangaManagementSystem.Infrastructure/Repositories/SeriesProposalRepository.cs` | Implemented `IsActiveTantouEditorContributorAsync` (EF AsNoTracking). Wrapped all four editorial-review SP calls in `try/catch(SqlException)` and added `MapReviewSqlException` that maps known SQL error numbers (573xx–576xx, 2627, 2601, 547) to user-safe `InvalidOperationException` messages |
| `src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalList.razor` | Rewrote to use `IEditorProposalApiClient`. Added Reviewer/Reviewed columns. Fixed navigation bug (was passing `SeriesId` instead of `SeriesProposalId`). Removed placeholder Reject button |
| `src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalReviewDetail.razor` | Rewrote to use `IEditorProposalApiClient`. Read-only snapshot with proposal/markup file links. Three MudDialog modals (Request Revision, Pass To Board, Cancel Proposal). Actions gated by server-computed permission flags (`CanClaim`, `CanRequestRevision`, `CanPassToBoard`, `CanCancel`) |
| `src/MangaManagementSystem.Web/Program.cs` | Registered `IEditorProposalApiClient` / `EditorProposalApiClient` via `AddHttpClient` with `ApiSettings.BaseUrl` |

### Added

| File | Purpose |
|------|---------|
| `src/MangaManagementSystem.API/Contracts/EditorProposalRequests.cs` | `ClaimProposalRequest`, `RequestRevisionForm`, `PassToBoardForm`, `CancelProposalForm` (IFormFile stays in API boundary only) |
| `src/MangaManagementSystem.API/Controllers/Editor/EditorProposalsController.cs` | Thin controller. Reads route/query/form/header, maps `IFormFile` to `byte[]` + name + content type, dispatches via `IMediator.Send(...)`. Maps `InvalidOperationException` to HTTP 400, other exceptions to HTTP 500 |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Queries/GetEditorialProposalQueue/GetEditorialProposalQueueQuery.cs` | Query record |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Queries/GetEditorialProposalQueue/GetEditorialProposalQueueQueryHandler.cs` | Maps queue entities to `ProposalQueueItemDto` |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Queries/GetEditorProposalDetail/GetEditorProposalDetailQuery.cs` | Query record (proposalId + actorUserId) |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Queries/GetEditorProposalDetail/GetEditorProposalDetailQueryHandler.cs` | Builds `EditorProposalDetailDto` with computed permission flags |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Commands/ClaimEditorialReview/ClaimEditorialReviewCommand.cs` | Command record |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Commands/ClaimEditorialReview/ClaimEditorialReviewCommandHandler.cs` | Validates inputs, calls `ClaimEditorialReviewAsync` |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Commands/RequestProposalRevision/RequestProposalRevisionCommand.cs` | Command record (comments required, markup optional) |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Commands/RequestProposalRevision/RequestProposalRevisionCommandHandler.cs` | Validates comments, uploads optional markup to Cloudinary, calls `RequestRevisionAsync`, cleans up on failure |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Commands/PassProposalToBoard/PassProposalToBoardCommand.cs` | Command record (comments optional, markup optional) |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Commands/PassProposalToBoard/PassProposalToBoardCommandHandler.cs` | Uploads optional markup, calls `PassToBoardAsync`, cleans up on failure |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Commands/CancelProposalReview/CancelProposalReviewCommand.cs` | Command record (comments required, markup required) |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Commands/CancelProposalReview/CancelProposalReviewCommandHandler.cs` | Validates comments + markup, uploads markup, calls `CancelProposalAsync`, cleans up on failure |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/Common/EditorialMarkupUploader.cs` | Shared helper: validates markup (type/size), uploads to Cloudinary via `IFileStorageService`, best-effort cleanup on failure |
| `src/MangaManagementSystem.Application/Features/Editor/SeriesProposals/EditorialMarkupValidation.cs` | Shared validation rules: allowed extensions (`.pdf/.doc/.docx/.jpg/.jpeg/.png/.webp`), allowed MIME types, 10 MB cap, resource-type resolver (image vs raw) |
| `src/MangaManagementSystem.Web/Services/Api/IEditorProposalApiClient.cs` | Interface for the typed Web-to-API client |
| `src/MangaManagementSystem.Web/Services/Api/EditorProposalApiClient.cs` | HttpClient-backed implementation with multipart form building, safe error parsing, `X-Actor-User-Id` header forwarding |

## API Endpoints Added

All endpoints are under `api/editor/proposals` and use the transitional `X-Actor-User-Id`
header pattern (same as the Mangaka workflows) to forward the logged-in user's id from the
Web host.

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/editor/proposals?status=UNDER_EDITORIAL_REVIEW` | Editorial review queue (optional status filter) |
| `GET` | `/api/editor/proposals/{proposalId}` | Proposal detail with computed permission flags |
| `POST` | `/api/editor/proposals/{proposalId}/claims` | Claim review (JSON body: optional `notes`) |
| `POST` | `/api/editor/proposals/{proposalId}/revision-requests` | Request Revision (multipart: `comments` required, `markupFile` optional) |
| `POST` | `/api/editor/proposals/{proposalId}/board-submissions` | Pass To Board (multipart: `comments` optional, `markupFile` optional) |
| `POST` | `/api/editor/proposals/{proposalId}/cancellations` | Cancel Proposal (multipart: `comments` required, `markupFile` required) |

## Commands and Queries Added

| Name | Type | Purpose |
|------|------|---------|
| `GetEditorialProposalQueueQuery` | Query | Reads the editorial queue (filterable by status) via EF AsNoTracking and maps to `ProposalQueueItemDto` |
| `GetEditorProposalDetailQuery` | Query | Reads a single proposal with full snapshot data and computes permission flags (`CanClaim`, `CanRequestRevision`, `CanPassToBoard`, `CanCancel`) for the current actor |
| `ClaimEditorialReviewCommand` | Command | Claims a proposal for editorial review by calling `usp_SeriesProposal_ClaimEditorialReview` (no file upload) |
| `RequestProposalRevisionCommand` | Command | Validates comments, uploads optional markup to Cloudinary, calls `usp_SeriesProposal_RequestRevision`, cleans up on failure |
| `PassProposalToBoardCommand` | Command | Uploads optional markup, calls `usp_SeriesProposal_PassToBoard` (never sets APPROVED), cleans up on failure |
| `CancelProposalReviewCommand` | Command | Validates comments + markup, uploads markup, calls `usp_SeriesProposal_CancelEditorialReview`, cleans up on failure |

## Repository Changes

### Existing SP wrappers used for write transitions (no changes to method signatures)

- `ClaimEditorialReviewAsync` — wraps `manga.usp_SeriesProposal_ClaimEditorialReview`
- `RequestRevisionAsync` — wraps `manga.usp_SeriesProposal_RequestRevision`
- `PassToBoardAsync` — wraps `manga.usp_SeriesProposal_PassToBoard`
- `CancelProposalAsync` — wraps `manga.usp_SeriesProposal_CancelEditorialReview`

All four were updated to wrap `ExecuteSqlRawAsync` in `try/catch(SqlException)` and map
known error numbers to user-safe `InvalidOperationException` messages via a new private
`MapReviewSqlException` method. This keeps SQL error mapping in Infrastructure (matching
the existing `MapSqlException` pattern for `usp_SeriesProposal_Submit`) and prevents
`Microsoft.Data.SqlClient` from leaking into the Application layer.

### New method added

- `IsActiveTantouEditorContributorAsync(Guid seriesId, Guid userId, CancellationToken)` —
  EF `AsNoTracking` query that checks whether the specified user is an active Tantou Editor
  contributor of the given series. Mirrors the membership predicate used by the editorial-review
  stored procedures and represents the "claimed" state.

### Read models

- Queue: `GetEditorialQueueAsync` (existing, EF AsNoTracking via `Include`/`ThenInclude`)
- Detail: `GetByIdWithDetailsAsync` (existing, EF AsNoTracking via `Include`/`ThenInclude`)

### SQL stored procedures

**No SQL stored procedure changes were required.** All four editorial SPs already existed
with the correct contracts, eligibility guards, and audit events.

## UI Behavior

### `/editor/proposals` (Proposal Review Queue)

- Loads the proposal queue through `IEditorProposalApiClient` (no direct Application injection).
- Status filter dropdown (default: `UNDER_EDITORIAL_REVIEW`).
- Refresh button reloads the queue.
- Empty state shows "All Caught Up!" when no proposals match the filter.
- Columns: Series title + proposal title, version, submitter, submitted date, reviewer,
  reviewed date, status chip, Review action button.
- **Fixed navigation bug**: Review button now passes `SeriesProposalId` (was incorrectly
  passing `SeriesId`).
- Removed the placeholder "Reject" button (not part of this workflow).

### `/editor/proposals/{proposalId}` (Proposal Review Detail)

- Read-only proposal snapshot: series title, series slug, proposal version, proposal title,
  genre snapshot, synopsis snapshot, status chip, submitter, submitted timestamp.
- Proposal file link opens in a new browser tab (`target="_blank"`).
- Markup file link appears only when `MarkupFileId` / `MarkupFileUrl` is present.
- After an editorial decision, the decision section shows reviewer name, reviewed timestamp,
  comments, and markup file link.
- Action buttons are gated by server-computed permission flags:
  - `CanClaim` → shows "Claim Review" button
  - `CanRequestRevision` → shows "Request Revision" button
  - `CanPassToBoard` → shows "Pass To Board" button
  - `CanCancel` → shows "Cancel Proposal" button
- After any editorial decision (`HasEditorialDecision == true`), all action buttons are hidden.
- Back button returns to `/editor/proposals`.

## Review Actions

| Action | Comments | Markup | Resulting Status | Stored Procedure |
|--------|----------|--------|------------------|------------------|
| **Claim Review** | n/a | n/a | Stays `UNDER_EDITORIAL_REVIEW` | `usp_SeriesProposal_ClaimEditorialReview` |
| **Request Revision** | Required | Optional | `REVISION_REQUESTED` (series → `PROPOSAL_DRAFT`) | `usp_SeriesProposal_RequestRevision` |
| **Pass To Board** | Optional | Optional | `UNDER_BOARD_REVIEW` (series → `UNDER_BOARD_REVIEW`) | `usp_SeriesProposal_PassToBoard` |
| **Cancel Proposal** | Required | Required | `CANCELLED` (series → `CANCELLED`) | `usp_SeriesProposal_CancelEditorialReview` |

Each action is implemented as a MudDialog modal with client-side validation before the API
call. Success shows a snackbar and reloads the detail. Errors show friendly messages from
the API (never raw SQL).

## Validation Rules

- **Request Revision**: comments required (non-empty); markup file optional.
- **Pass To Board**: comments optional; markup file optional; transitions to
  `UNDER_BOARD_REVIEW` only — **never sets `APPROVED`** (approval comes from board result).
- **Cancel Proposal**: comments required (non-empty); markup file required.
- **After any editorial decision** (`ReviewedAtUtc` has a value): all action buttons are
  hidden/disabled. The proposal snapshot remains read-only and immutable.
- **Board rejection reasons** remain in the board poll/vote workflow, not in editorial
  comments.
- **Markup file validation** (Application handler + UI):
  - Allowed extensions: `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp`
  - Allowed MIME types: `application/pdf`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `image/jpeg`, `image/png`, `image/webp`
  - Max size: 10 MB
  - Invalid type/size rejected with friendly message; no raw SQL or stack traces surfaced.
- **Claim eligibility**: only proposals with `StatusCode == UNDER_EDITORIAL_REVIEW` and no
  existing editorial decision can be claimed. The stored procedure is the authoritative
  backstop.
- **Decision eligibility**: only the active Tantou Editor contributor (the one who claimed)
  can record a decision. The stored procedure enforces this.
- **WITHDRAWN**: not implemented in this task. Mangaka withdrawal is out of scope.

## Build Result

```
dotnet build MangaManagementSystem.slnx
```

- **Build succeeded**
- **0 errors**
- **58 warnings**

The previous session had approximately 61 pre-existing warnings. The current 58 warnings
are the same categories as before: `MUD002`, `CS0649`, `CS0414`, `CS8981`. A filtered
check for warnings in the new Editor files produced no output, confirming that **no
warnings were introduced by this task**.

## Manual Test Checklist

1. [ ] Login as active Tantou Editor and open `/editor/proposals`.
2. [ ] Proposal queue loads.
3. [ ] Status filter and refresh work.
4. [ ] Empty state appears when no proposals match.
5. [ ] Columns show proposal/series/version/submitter/reviewer/status metadata.
6. [ ] Review button navigates to `/editor/proposals/{seriesProposalId}` (correct id).
7. [ ] Detail page shows read-only proposal snapshot.
8. [ ] Proposal file link opens in new tab.
9. [ ] Eligible unclaimed proposal shows Claim action.
10. [ ] Claim succeeds and reloads detail.
11. [ ] Request Revision blocks empty comments (client-side).
12. [ ] Request Revision succeeds with comments and sets `REVISION_REQUESTED`.
13. [ ] Pass To Board succeeds and sets `UNDER_BOARD_REVIEW`, not `APPROVED`.
14. [ ] Cancel blocks missing comments or missing markup (client-side).
15. [ ] Cancel succeeds with comments + markup and sets `CANCELLED`.
16. [ ] After a decision, action buttons are hidden/disabled.
17. [ ] Comments and markup link render after review.
18. [ ] Build remains successful.

## Remaining Tasks

- Manual browser/database verification still recommended (end-to-end with real Tantou Editor
  login, real proposals, real Cloudinary uploads).
- Download proxy with forced attachment remains future work if file buttons currently open
  direct Cloudinary URLs (same as other file-download patterns in the project).
- Legacy `ISeriesProposalService` remains registered in DI for transitional compatibility
  but is no longer used by the migrated Editor pages. Safe to remove in a future cleanup
  pass after confirming no other consumers exist.
