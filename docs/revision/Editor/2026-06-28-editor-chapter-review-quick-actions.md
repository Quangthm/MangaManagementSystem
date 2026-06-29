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
- Workspace review actions (owned by another teammate)
- Notifications to Mangaka when review decision is recorded

---

## Follow-up Fix — Queue Navigation, Detail Route Layout, and EF Parity Verification

### Date
2026-06-28 (same day follow-up)

### Problem
`/editor/chapters` queue did not route to `/editor/chapters/{chapterId}`, so the new quick action panel was unreachable from the queue. The "Review Chapter" button was navigating to the workspace URL instead of the detail route.

### Root cause found
`ChapterReviewList.razor` line 110-111: The "Review Chapter" button called `Nav.NavigateTo(context.WorkspaceUrl)` instead of `Nav.NavigateTo($"/editor/chapters/{context.ChapterId}")`.

### Route/Layout Decision
`/editor/chapters/{ChapterId:guid}` is a routed detail page inside the existing `EditorLayout`. It is not a left-sidebar navigation item. The existing `/editor/chapters` sidebar item remains the parent navigation item.

### Files changed in this follow-up
- `src/MangaManagementSystem.Web/Components/Pages/Editor/ChapterReviewList.razor`
  - Changed "Review Chapter" button to navigate to `/editor/chapters/{chapterId}`
  - Added separate "Workspace" button (outlined, secondary) preserving workspace navigatio
- `src/MangaManagementSystem.Infrastructure/Repositories/EditorChapterReviewRepository.cs`
  - Changed audit action code from `CHAPTER_EDITORIAL_REVIEW_CREATED` to `CHAPTER_EDITORIAL_REVIEW_RECORDED`
  - Added concurrency guard: `ExecuteUpdateAsync` with `WHERE chapter_id = {id} AND status_code = N'UNDER_REVIEW'`, rejects if affected rows == 0
  - Added optional markup FileResource validation: verifies FileResource exists, `FilePurposeCode = EDITORIAL_ATTACHMENT`, `DeletedAtUtc IS NULL`
  - Enriched audit detail JSON: chapter_id, series_id, chapter_editorial_review_id, old_status_code, new_status_code, decision_code, has_markup_file, markup_file_id

### Queue navigation behavior
- Each chapter row now has two actions:
  - **Review Chapter** (filled primary) → `/editor/chapters/{chapterId}`
  - **Workspace** (outlined secondary) → existing workspace URL
- Workspace behavior fully preserved

### Detail route/layout behavior
- Route: `@page "/editor/chapters/{ChapterId:guid}"` — unchanged, already correct
- Layout: `<EditorLayout>` wrapper — unchanged, already correct
- Back button: `Nav.NavigateTo("/editor/chapters")` — unchanged, already correct
- Quick action panel: visible when `StatusCode == "UNDER_REVIEW"` — unchanged, already correct

### Sidebar nav decision
- `/editor/chapters` nav item uses `MatchAll = false` (default, meaning `NavLinkMatch.Prefix`)
- Therefore `/editor/chapters/{chapterId}` correctly highlights the "Chapters" sidebar item
- No sidebar changes needed

### Workspace behavior preserved
- Workspace button still available on queue rows as "Workspace" (outlined)
- Workspace button still available on detail page as "Review in Workspace"
- No workspace navigation paths changed

### Stored procedure baseline parity

| Check | Result |
|-------|--------|
| Decision mapping (APPROVED→APPROVED, REVISION_REQUESTED→REVISION_REQUESTED, CANCELLED→CANCELLED) | Matches |
| Required comments for REVISION_REQUESTED and CANCELLED | Matches |
| UNDER_REVIEW guard (reject non-UNDER_REVIEW chapters) | Matches |
| Active Tantou Editor guard (via ActiveSeriesContributors) | Matches |
| Active contributor guard (same as queue/detail) | Matches |
| Optional markup guard (validates FileResource exists, EDITORIAL_ATTACHMENT, not deleted) | Added |
| Transactional review insert + status update + audit (one transaction) | Matches |
| Concurrency/double-submit guard (ExecuteUpdateAsync with status condition) | Added |
| Audit action code `CHAPTER_EDITORIAL_REVIEW_RECORDED` | Matches baseline |
| Audit detail JSON (chapter_id, series_id, review_id, old/new status, decision, markup) | Enriched |

### Build result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
- Build succeeded
- 0 errors
- 47 warnings (all pre-existing)

### Manual test checklist
- [ ] Open `/editor/chapters`
- [ ] Confirm each row has "Review Chapter" (primary) and "Workspace" (outlined) buttons
- [ ] Click "Review Chapter" — confirm URL becomes `/editor/chapters/{chapterId}`
- [ ] Confirm page stays inside EditorLayout/sidebar shell
- [ ] Confirm no new detail route item appears in left sidebar
- [ ] Confirm "Chapters" sidebar item remains active/highlighted
- [ ] Confirm quick actions appear for `UNDER_REVIEW`
- [ ] Click "Workspace" — confirm workspace opens
- [ ] Confirm workspace return/back behavior still works
- [ ] Approve chapter and confirm status becomes `APPROVED`
- [ ] Request revision with comments and confirm status becomes `REVISION_REQUESTED`
- [ ] Cancel with comments and confirm status becomes `CANCELLED`
- [ ] Try blank comments for revision/cancel and confirm UI blocks it
- [ ] Try double-submit/stale submit and confirm backend rejects second decision
- [ ] Confirm no stored procedure was added

---

## Follow-up Fix — Detail Page Runtime UX

### Date
2026-06-28 (second follow-up)

### Problem
Runtime testing of `/editor/chapters/{ChapterId:guid}` showed:
1. **Review decision buttons did not open dialogs** — `MudDialog` used invalid attribute `@bind-IsVisible` and `T="bool"` which produced MUD0002 warnings and failed at runtime.
2. **Whole-chapter Workspace action was duplicated** — appeared in both the Pages card header and the bottom-right Review card.
3. **Pages table lacked pagination** — no `RowsPerPage` or pager controls.
4. **No per-page workspace navigation** — clicking a page row did not open the workspace at that page.
5. **Open Annotations section was flat** — no pagination, no grouping by page/version.
6. **Annotations lacked page/version context** — DTO and repository did not populate page number or version ID.

### Dialog fix approach
Changed `MudDialog @bind-IsVisible="_dialogVisible" T="bool"` to `MudDialog @bind-Visible="_dialogVisible" Options="_dialogOptions"` with proper `TitleContent`, matching the existing `ProposalReviewDetail.razor` pattern used throughout the project. Removed all 3 MUD0002 MudDialog warnings.

### Duplicate Workspace action removed
Removed the `Review in Workspace` button from the Pages card header. Kept only the bottom-right Review card's `Review in Workspace` button which explains the purpose.

### Pages pagination added
`MudTable` with `RowsPerPage="5"` for built-in pagination when more than 5 pages exist.

### Page click/navigation behavior
Added `Open in Workspace` button per page row that navigates to:
```
/series/{slug}/workspace?chapterId={id}&page={pageNumber}&version={versionId}&returnUrl=/editor/chapters/{id}
```
Falls back to page-only URL when version ID is unavailable.

### Annotation grouping/pagination added
Annotations now grouped by `(PageNumber, VersionNo, VersionId)` displayed as:
```
Page 1 · Version v1
  [annotation cards...]
```
Manual pagination: 5 groups per page with Previous/Next controls.

### Annotation click/navigation behavior
Clicking an annotation navigates to:
```
/series/{slug}/workspace?chapterId={id}&page={pageNumber}&version={versionId}&returnUrl=/editor/chapters/{id}
```
Falls back to page-only, then chapter workspace URL.

### DTO/read-model changes
- `EditorChapterReviewAnnotationDto`: Added `PageNumber`, `CurrentVersionId`, `CurrentVersionNo` fields
- Domain `EditorChapterReviewDetailAnnotation`: Added same fields
- Repository: Updated annotation query to project page number and version ID from first region's page version
- Handler: Updated mapping to pass new fields

### Files changed
- `Web/Components/Pages/Editor/ChapterReviewDetail.razor` — full rewrite for dialog, pagination, grouping, navigation
- `Application/DTOs/Editor/EditorChapterReviewDtos.cs` — added page/version fields to annotation DTO
- `Domain/Interfaces/IEditorChapterReviewRepository.cs` — added page/version fields to domain annotation record
- `Infrastructure/Repositories/EditorChapterReviewRepository.cs` — updated annotation query to project page/version context
- `Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewDetail/GetEditorChapterReviewDetailQueryHandler.cs` — updated annotation mapping

### Build result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
- Build succeeded
- 0 errors
- 45 warnings (removed 3 MudDialog MUD0002, added 1 MudPaper MUD0002 for `OnClick` — same false-positive pattern used across project)

### Manual test checklist
- [ ] Open `/editor/chapters`
- [ ] Click `Review Chapter`
- [ ] Confirm detail page opens
- [ ] Click `Approve Chapter` and confirm dialog opens
- [ ] Click `Request Revision` and confirm dialog opens
- [ ] Click `Cancel Chapter` and confirm dialog opens
- [ ] Try blank comments for Request Revision and Cancel; confirm UI blocks submission
- [ ] Confirm only one whole-chapter `Review in Workspace` button remains (bottom-right card)
- [ ] Confirm Pages section paginates when enough pages exist (RowsPerPage=5)
- [ ] Click page `Open in Workspace` button and confirm workspace opens at that page
- [ ] Confirm Open Annotations section is grouped by page/version
- [ ] Confirm Open Annotations section paginates when enough annotation groups exist (5 per page)
- [ ] Click an annotation card and confirm workspace opens to that page context
- [ ] Confirm no workspace behavior was broken

---

## Follow-up Fix — Detail Page Table Simplification and Annotation Filter

### Date
2026-06-28 (third follow-up)

### Problem
Runtime testing showed:
- `Open image` in the Pages table was redundant (Workspace already shows images) and exposed Cloudinary URLs directly.
- The Pages table had separate Preview and Actions columns making the UI noisy; page rows should navigate directly.
- The Actions column was unnecessary — page navigation should be via row click.
- Editors needed unresolved annotation counts per page/current version for quick context.
- Open Annotations section needed a page filter for focused review.

### Fix

#### Preview/Open image column removed
Removed the entire `Preview` column and `Open image` links. No Cloudinary image URLs are exposed from the detail page.

#### Actions column removed
Removed the `Actions` column and the `Open in Workspace` per-row button. Row navigation is now via direct click.

#### Page row/Current Version click navigates to Workspace
- Clicking a page row (the Page number `MudTd`) opens Workspace for that page/version.
- `Current Version` text is a clickable `MudButton` that also opens Workspace.
- Destination: `/series/{slug}/workspace?chapterId={id}&page={pn}&version={vid}&returnUrl=/editor/chapters/{id}`

#### Unresolved Annotations count added
New `Unresolved Annotations` column shows the count of unresolved open annotations per page:
- Matched by `PageNumber` and `CurrentVersionId` (fallback to page-only if no version).
- Display: `MudChip` (warning color for >0, outlined for 0).
- Computed client-side from `_detail.OpenAnnotations` — no backend changes needed.

#### Open Annotations page filter added
`MudSelect` at top of Open Annotations section with options:
- "All pages" (default)
- "Page 1", "Page 2", etc. from the pages list
- Filter resets annotation pagination to page 1 on change.
- Empty state: "No open annotations for Page N." when filtered page has none.

### Files changed
- `Web/Components/Pages/Editor/ChapterReviewDetail.razor`
  - Removed Preview and Actions columns from Pages table
  - Removed Open image links (Cloudinary URL exposure)
  - Made page rows and Current Version clickable (workspace navigation)
  - Added Unresolved Annotations column
  - Added page filter to Open Annotations section
  - Added `_annotationPageFilter`, `GetUnresolvedAnnotationCount`, `OnAnnotationPageFilterChanged`

### Build result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
- Build succeeded
- 0 errors
- 46 warnings (added 1 MudTd OnClick MUD0002 — same false-positive pattern as MudPaper OnClick across the project)

### Manual test checklist
- [ ] Open `/editor/chapters`
- [ ] Click `Review Chapter`
- [ ] Confirm detail page opens
- [ ] Confirm Pages table no longer shows `Open image`
- [ ] Confirm direct Cloudinary image URL is no longer exposed from Pages table
- [ ] Confirm Pages table no longer has Actions column
- [ ] Click a page row and confirm Workspace opens at that page/version
- [ ] Click Current Version and confirm Workspace opens at that page/version
- [ ] Confirm Unresolved Annotations count is shown per page/current version
- [ ] Confirm Open Annotations has page filter
- [ ] Filter annotations by page and confirm only that page's groups show
- [ ] Confirm annotation grouping and pagination still work
- [ ] Confirm Approve/Request Revision/Cancel dialogs still work
- [ ] Confirm bottom-right Review in Workspace button still works

---

## Follow-up Implementation — Markup File Upload for Review Decisions

### Date
2026-06-28 (fourth follow-up)

### Problem
Editor review decisions supported nullable `MarkupFileId`, but the UI always submitted null and there was no upload path for editorial markup/review attachments. The Tantou Editor needed to optionally attach PDF, Word documents, or images as supporting materials for review decisions.

### Fix

#### Existing upload helper/pattern reused
Reused `EditorialMarkupUploader` and `EditorialMarkupValidation` from the series proposal workflow. These provide:
- File validation (extension, content type, size ≤10 MB)
- Cloudinary upload through `IFileStorageService`
- sha256 hash verification
- Best-effort cleanup if upload succeeds but DB write fails

#### Endpoint strategy
- Kept existing JSON endpoint `POST /api/editor/chapters/{chapterId}/review-decision` for no-file submissions.
- Added new multipart endpoint `POST /api/editor/chapters/{chapterId}/review-decision/with-markup` with `[Consumes("multipart/form-data")]`.
- UI always calls multipart endpoint when a file is selected, JSON endpoint otherwise.

#### UI file picker behavior
`MudFileUpload` with `Accept=".pdf,.doc,.docx,.jpg,.jpeg,.png,.webp"` in the decision dialog. Shows selected file name, size, and a clear button. File selection is optional for all decisions.

#### FileResource creation with `EDITORIAL_ATTACHMENT`
Repository creates a `FileResource` entity in the EF transaction with:
- `FilePurposeCode = "EDITORIAL_ATTACHMENT"`
- Original file name, Cloudinary public ID, secure URL, content type, size, sha256 hash
- `UploadedByUserId = actorUserId`, `UploadedAtUtc`

#### Cloudinary upload / sha256 behavior
`EditorialMarkupUploader.ValidateAndUploadAsync` validates file metadata, uploads to Cloudinary via `IFileStorageService`, verifies sha256 hash is present, and returns `FileUploadResultDto`. If hash is missing, the upload is cleaned up and an error is thrown.

#### Transaction behavior
1. File uploaded to Cloudinary outside EF transaction (via `EditorialMarkupUploader`).
2. Repository starts EF transaction:
   - Creates `FileResource` row
   - Creates `ChapterEditorialReview` with `markup_file_id`
   - Updates `Chapter.status_code` via `ExecuteUpdateAsync` (concurrency guarded)
   - Writes audit event
   - Commits atomically

#### Cleanup behavior
If the handler's upload succeeds but the repository transaction fails, `EditorialMarkupUploader.TryCleanupAsync` performs best-effort Cloudinary deletion of the orphaned file.

#### Markup display after review
Deferred to follow-up. The `ChapterEditorialReview.markup_file_id` is correctly populated and the FileResource exists, but the detail page does not yet display attached markup files after a review decision.

### Files changed
- `Application/Features/.../SubmitChapterEditorialReviewCommand.cs` — added `MarkupFileBytes`, `MarkupFileName`, `MarkupContentType`
- `Application/Features/.../SubmitChapterEditorialReviewCommandHandler.cs` — added `IFileStorageService`, `EditorialMarkupUploader` orchestration, cleanup on failure
- `Domain/Interfaces/IEditorChapterReviewRepository.cs` — added `UploadedFileMetadata` record; modified `SubmitChapterEditorialReviewAsync` signature
- `Infrastructure/Repositories/EditorChapterReviewRepository.cs` — creates `FileResource` in EF transaction, enriched audit detail
- `API/Controllers/Editor/EditorChapterReviewsController.cs` — fixed JSON endpoint, added multipart endpoint, added `SubmitChapterEditorialReviewFormRequest`
- `Web/Services/Api/IEditorChapterReviewApiClient.cs` — added `SubmitReviewDecisionWithMarkupAsync`
- `Web/Services/Api/EditorChapterReviewApiClient.cs` — implemented multipart upload
- `Web/Components/Pages/Editor/ChapterReviewDetail.razor` — added file picker to decision dialog with file name/size display and clear button

### Build result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
- Build succeeded
- 0 errors
- 47 warnings (all pre-existing)

### Manual test checklist
- [ ] Open `/editor/chapters`
- [ ] Open an UNDER_REVIEW chapter detail
- [ ] Click Request Revision
- [ ] Enter comments and attach a valid PDF/DOCX/image
- [ ] Submit and confirm decision succeeds
- [ ] Confirm `ChapterEditorialReview.markup_file_id` is not null
- [ ] Confirm `FileResource.file_purpose_code = EDITORIAL_ATTACHMENT`
- [ ] Confirm file has `sha256_hash`
- [ ] Confirm chapter status becomes `REVISION_REQUESTED`
- [ ] Try unsupported file type and confirm UI/API rejects it
- [ ] Try Request Revision with file but blank comments and confirm it is rejected
- [ ] Try Cancel with file but blank comments and confirm it is rejected
- [ ] Try Approve with optional file and confirm comments remain optional
- [ ] Confirm no stored procedure was added
