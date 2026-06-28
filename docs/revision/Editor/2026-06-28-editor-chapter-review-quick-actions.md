# Editor Chapter Review Quick Actions

## Branch
`feature/Mangaka`

## Date
2026-06-28

## Scope
Added final chapter-level review decision quick actions (Approve / Request Revision / Cancel Chapter) directly on the existing Editor Chapter Review Detail page (`/editor/chapters/{ChapterId}`). Kept the existing Workspace button completely unchanged.

## Problem summary
The Editor chapter review detail page existed with read-only chapter overview, pages, annotations, and "Review in Workspace" buttons. It did not allow the Tantou Editor to record a final chapter-level editorial review decision directly on the page. The editor had to go to Workspace for review actions (owned by another teammate). Now editors can make quick final decisions directly from the detail page after inspecting the chapter overview.

## Latest EF Core direction
EF Core + Unit of Work with explicit transactions. No stored procedures added.

## Architecture flow

```
ChapterReviewDetail.razor
  -> IEditorChapterReviewApiClient.SubmitReviewDecisionAsync()
  -> POST /api/editor/chapters/{chapterId}/review-decision
  -> EditorChapterReviewsController.SubmitReviewDecisionAsync()
  -> IMediator.Send(SubmitChapterEditorialReviewCommand)
  -> SubmitChapterEditorialReviewCommandHandler
  -> IEditorChapterReviewRepository.SubmitChapterEditorialReviewAsync()
  -> EF Core transaction (ApplicationDbContext)
  -> SQL Server
```

## Files changed by layer

### Domain
- `src/MangaManagementSystem.Domain/Interfaces/IEditorChapterReviewRepository.cs`
  - Added `SubmitChapterEditorialReviewAsync` method
  - Added `ChapterEditorialReviewResult` record
  - Updated interface doc comment (no longer read-only)

### Application
- `src/MangaManagementSystem.Application/DTOs/Editor/EditorChapterReviewDtos.cs`
  - Added `SubmitChapterEditorialReviewRequest` record
  - Added `SubmitChapterEditorialReviewResponse` record
- `src/MangaManagementSystem.Application/Features/Editor/ChapterReviews/Commands/SubmitChapterEditorialReview/SubmitChapterEditorialReviewCommand.cs` (new)
- `src/MangaManagementSystem.Application/Features/Editor/ChapterReviews/Commands/SubmitChapterEditorialReview/SubmitChapterEditorialReviewCommandHandler.cs` (new)

### Infrastructure
- `src/MangaManagementSystem.Infrastructure/Repositories/EditorChapterReviewRepository.cs`
  - Added `SubmitChapterEditorialReviewAsync` — EF Core transactional write with audit

### API
- `src/MangaManagementSystem.API/Controllers/Editor/EditorChapterReviewsController.cs`
  - Added `SubmitReviewDecisionAsync` endpoint + import for command namespace

### Web
- `src/MangaManagementSystem.Web/Services/Api/IEditorChapterReviewApiClient.cs`
  - Added `SubmitReviewDecisionAsync` method
- `src/MangaManagementSystem.Web/Services/Api/EditorChapterReviewApiClient.cs`
  - Implemented `SubmitReviewDecisionAsync`
- `src/MangaManagementSystem.Web/Components/Pages/Editor/ChapterReviewDetail.razor`
  - Added review decision quick action panel (visible only when status_code == UNDER_REVIEW)
  - Added decision dialog (MudDialog) with per-decision UI
  - Added `ISnackbar` for success/error feedback
  - Added `_submitting` loading state
  - Kept existing Workspace buttons unchanged

## Endpoint added
- `POST /api/editor/chapters/{chapterId}/review-decision`
  - Requires `X-Actor-User-Id` header
  - Body: `SubmitChapterEditorialReviewRequest` (DecisionCode, Comments, MarkupFileId)
  - Returns 200 with `SubmitChapterEditorialReviewResponse` on success
  - Returns 400 on validation failure (wrong status, unauthorized, etc.)
  - Returns 500 on unexpected errors

## Command/handler added
- `SubmitChapterEditorialReviewCommand` (ActorUserId, ChapterId, DecisionCode, Comments, MarkupFileId)
- `SubmitChapterEditorialReviewCommandHandler`
  - Validates ActorUserId, ChapterId, DecisionCode
  - Validates allowed decisions: APPROVED, REVISION_REQUESTED, CANCELLED
  - Requires non-blank comments for REVISION_REQUESTED and CANCELLED
  - Enforces 2000-char max on comments

## Repository / Unit of Work behavior
Single EF transaction in `SubmitChapterEditorialReviewAsync`:
1. Loads chapter with series
2. Verifies actor is active Tantou Editor contributor via `ActiveSeriesContributors`
3. Verifies chapter is `UNDER_REVIEW`
4. Creates `ChapterEditorialReview` entity
5. Updates `Chapter.StatusCode` according to decision
6. Creates `AuditEvent` with action code `CHAPTER_EDITORIAL_REVIEW_CREATED`
7. Single `SaveChangesAsync` + commit

## Audit behavior
EF-based audit in the same transaction (following `QuickSelectRepository` pattern):
- Action code: `CHAPTER_EDITORIAL_REVIEW_CREATED`
- Entity type: `Chapter`
- Entity ID: `chapterId`
- Detail JSON: decision, reviewer, previous status, new status
- Actor role name resolved from `ActiveSeriesContributors`

## UI behavior

### Quick action panel
- Visible only when `Chapter.status_code == UNDER_REVIEW`
- Three buttons: Approve Chapter (green), Request Revision (orange), Cancel Chapter (red)
- Buttons disabled while submitting
- Progress bar shown during submission

### Decision dialogs

#### Approve
- Comments optional
- Helper text: chapter will become APPROVED
- Confirm button: "Approve Chapter"

#### Request Revision
- Comments required (validated client-side before API call)
- Helper text: explains revision vs. cancellation
- Confirm button: "Request Revision"

#### Cancel
- Comments required (validated client-side before API call)
- Error alert with strong warning about terminal nature
- Helper text: use revision instead if fixable
- Confirm button: "Cancel Chapter"

### Post-decision behavior
- On success: snackbar notification, page reloads detail (GET /review-detail)
- Quick action panel hides because chapter is no longer UNDER_REVIEW
- On failure: error snackbar with safe message

## Validation rules
- DecisionCode must be exactly APPROVED, REVISION_REQUESTED, or CANCELLED
- REVISION_REQUESTED and CANCELLED require non-blank comments
- Chapter must be UNDER_REVIEW
- Actor must be active Tantou Editor contributor of the chapter's series
- Comments capped at 2000 characters
- MarkupFileId passed as null (markup upload deferred)

## Markup file upload
Deferred to follow-up. UI passes `MarkupFileId = null`. Backend accepts nullable `MarkupFileId`.

## Build result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
- Build succeeded
- 0 errors
- 47 warnings (all pre-existing; 3 new MUD0002 warnings on ChapterReviewDetail.razor match existing MudBlazor analyzer pattern)

## Manual test checklist
- [ ] Open Editor chapter review queue (`/editor/chapters`)
- [ ] Click "Review Chapter" to open detail page
- [ ] Confirm Workspace button still works
- [ ] Confirm quick action buttons (Approve / Request Revision / Cancel) appear for UNDER_REVIEW chapter
- [ ] Click Approve, confirm in dialog, confirm status becomes APPROVED, quick actions hide
- [ ] Open another UNDER_REVIEW chapter, click Request Revision, enter comments, confirm
- [ ] Confirm status becomes REVISION_REQUESTED, quick actions hide
- [ ] Open another UNDER_REVIEW chapter, click Cancel, enter comments, confirm
- [ ] Confirm status becomes CANCELLED, quick actions hide
- [ ] Try Request Revision with empty comments — confirm dialog blocks it
- [ ] Try Cancel with empty comments — confirm dialog blocks it
- [ ] Open a non-UNDER_REVIEW chapter detail — confirm quick actions are hidden
- [ ] Confirm Mangaka `/mangaka/chapters` shows latest feedback for revision requested chapter
- [ ] Confirm no Workspace behavior changed
- [ ] Confirm no stored procedure was added

## Known issues
- The MUD0002 warnings on `MudDialog` attributes (`T`, `IsVisible`, `IsVisibleChanged`) are pre-existing MudBlazor analyzer pattern warnings; the dialog functions correctly at runtime
- No `SCHEDULED` status chip color was defined in the original `GetStatusColor` — added `SCHEDULED` → `Color.Tertiary` and `CANCELLED` → `Color.Dark`

## Remaining follow-ups
- Markup file upload for editorial review decisions
- Audit action code enforcement if team confirms action code constraints
- Workspace review actions (owned by another teammate)
- Notifications to Mangaka when review decision is recorded
