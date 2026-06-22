# Chapter Review Detail — Real Data + Scoped Access

## Branch
`feature/Mangaka`

## Date
2026-06-19

## Task Summary
Migrated `ChapterReviewDetail.razor` from mock data to real database data with enforced Tantou Editor access scoping. The detail page now loads chapter information, pages with current versions, and open annotations from the database. Access is scoped so a Tantou Editor can only view chapters from series where they are an active contributor. The list page was also updated to apply the same scope, ensuring consistency.

## Architecture Path
```
ChapterReviewDetail.razor
  → IEditorChapterReviewApiClient.GetReviewDetailAsync(actorUserId, chapterId)
  → GET /api/editor/chapters/{chapterId}/review-detail
  → EditorChapterReviewsController.GetReviewDetailAsync
  → IMediator.Send(GetEditorChapterReviewDetailQuery)
  → GetEditorChapterReviewDetailQueryHandler
  → IEditorChapterReviewRepository.GetReviewDetailForEditorAsync(chapterId, actorUserId)
  → EF Core AsNoTracking
  → SQL Server
```

## Mock Data Removed
- Hard-coded series title: "Crimson Moon Academy"
- Hard-coded chapter label: "Ch. 45"
- Hard-coded chapter title: "The Shadow Rises"
- Hard-coded status: "UNDER_REVIEW"
- Hard-coded page count: 24
- Hard-coded creator: "Ken Sato"
- Hard-coded submitted date: "Jun 4, 2026"
- Hard-coded annotation list (3 fake annotations with fake authors)
- Mock "Add Annotation" button
- Mock "Resolve Annotation" button
- Mock "Submit Decision" button with fake decision options
- Mock "Review History" timeline

## Files Changed

### Added
- `src/MangaManagementSystem.Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewDetail/GetEditorChapterReviewDetailQuery.cs`
- `src/MangaManagementSystem.Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewDetail/GetEditorChapterReviewDetailQueryHandler.cs`

### Modified
- `src/MangaManagementSystem.Application/DTOs/Editor/EditorChapterReviewDtos.cs` — added `EditorChapterReviewDetailDto`, `EditorChapterReviewPageDto`, `EditorChapterReviewAnnotationDto`
- `src/MangaManagementSystem.Domain/Interfaces/IEditorChapterReviewRepository.cs` — added `GetReviewDetailForEditorAsync` method and `EditorChapterReviewDetail` record
- `src/MangaManagementSystem.Infrastructure/Repositories/EditorChapterReviewRepository.cs` — implemented detail query with access scope, updated queue query to apply scope, added `ScopedSeriesIdsQuery` helper
- `src/MangaManagementSystem.API/Controllers/Editor/EditorChapterReviewsController.cs` — added `GetReviewDetailAsync` endpoint returning 403 for unauthorized access
- `src/MangaManagementSystem.Web/Services/Api/IEditorChapterReviewApiClient.cs` — added `GetReviewDetailAsync` method and `EditorChapterReviewDetailResult` record
- `src/MangaManagementSystem.Web/Services/Api/EditorChapterReviewApiClient.cs` — implemented detail API call with 403/404 handling
- `src/MangaManagementSystem.Web/Components/Pages/Editor/ChapterReviewDetail.razor` — replaced all mock data with typed client call, added loading/error/access-denied states, added "Review in Workspace" button
- `src/MangaManagementSystem.Domain/Entities/ChapterPageAnnotation.cs` — added `CreatedAtUtc` property (read-only, maps existing DB column)

## Endpoint Added
- `GET /api/editor/chapters/{chapterId}/review-detail`
  - Requires `X-Actor-User-Id` header
  - Returns 200 with `EditorChapterReviewDetailDto` if actor has access
  - Returns 403 with error message if actor lacks access
  - Does not leak chapter/series details in forbidden state

## Query/Repository Methods Added

### Application
- `GetEditorChapterReviewDetailQuery(ChapterId, ActorUserId)` — MediatR query
- `GetEditorChapterReviewDetailQueryHandler` — maps domain detail to DTO, constructs workspace URL

### Domain
- `IEditorChapterReviewRepository.GetReviewDetailForEditorAsync(chapterId, actorUserId)` — returns `EditorChapterReviewDetail?` (null if not found or unauthorized)
- `EditorChapterReviewDetail` record — chapter info, pages, open annotations

### Infrastructure
- `EditorChapterReviewRepository.GetReviewDetailForEditorAsync` — EF Core AsNoTracking query with access scope enforcement
- `ScopedSeriesIdsQuery(actorUserId)` — composable subquery for active Tantou Editor contributor scope (reused by both queue and detail)

## Access Scoping Rule
Both list and detail queries now enforce:
```
Chapter.SeriesId == SeriesContributor.SeriesId
AND SeriesContributor.UserId == actorUserId
AND SeriesContributor.EndDate IS NULL
AND SeriesContributor.User.StatusCode == "ACTIVE"
AND SeriesContributor.User.Role.RoleName == "Tantou Editor"
```

**List scoping was also updated** — the queue query now filters chapters to only those from series where the actor is an active Tantou Editor contributor. This ensures consistency between list and detail views.

## Data Mapping

### Chapter Detail
- `ChapterId`, `SeriesId`, `SeriesTitle`, `SeriesSlug`
- `ChapterNumberLabel`, `ChapterTitle`, `StatusCode`
- `PageCount` — count of non-deleted `ChapterPage` rows
- `CurrentVersionCount` — count of pages with a current version
- `CreatedAtUtc` — chapter creation timestamp
- `SubmittedByDisplayName` — `CreatedByUser.DisplayName`
- `AssignedEditorDisplayName` — always null (no editor assignment concept in schema)

### Pages
- `ChapterPageId`, `PageNumber`
- `CurrentVersionId` — `ChapterPageVersion.ChapterPageVersionId` where `IsCurrentVersion == true`
- `CurrentVersionFileUrl` — `ChapterPageVersion.PageFile.CloudinarySecureUrl`
- `CurrentVersionNo` — version number (not timestamp, since `ChapterPageVersion` has no timestamp)

### Open Annotations
- `AnnotationId`, `Comment` (maps `AnnotationText`), `IssueTypeCode`
- `CreatedAtUtc` — annotation creation timestamp
- `CreatedByDisplayName` — `AnnotatedByUser.DisplayName`
- `IsResolved` — always false (filtered to `ResolvedAtUtc == null`)
- Traversal: `ChapterPageAnnotation → PageRegions → ChapterPageVersion → ChapterPage.ChapterId == chapterId`

## Workspace Redirect Behavior
- Workspace URL constructed in handler: `/series/{slug}/workspace?chapterId={chapterId}`
- "Review in Workspace" button appears in two places:
  - Top of pages section
  - Right sidebar info panel
- Button uses `NavigationManager.NavigateTo(workspaceUrl)`
- Button disabled if slug is missing

## UI Behavior

### States
- **Loading**: indeterminate progress bar
- **Access Denied**: centered card with block icon, "You do not have access to this chapter review" message, "Back to Chapters" button
- **Error**: red alert with error message and "Retry" button
- **Success**: full detail view

### Layout
- **Header**: "Back to Chapters" button, "Chapter Review" title, subtitle
- **Title bar**: series title + chapter label, chapter title (if present), status chip
- **Left column (8/12)**:
  - Pages section with "Review in Workspace" button
  - Pages table (page number, current version, preview link)
  - Open annotations list (issue type chip, comment, author + timestamp)
- **Right column (4/12)**:
  - Chapter info card (series, chapter, title, status, pages, versions, created, creator)
  - Review card with "Review in Workspace" button

### Empty States
- "No pages have been uploaded for this chapter yet."
- "No open annotations for this chapter."

## Build Result
```
dotnet build MangaManagementSystem\MangaManagementSystem.sln
```
- **Build succeeded**
- **0 errors**
- **0 warnings** (filtered check for new files produced no output)

## Manual Test Checklist
1. [ ] Login as active Tantou Editor who is an active contributor of a target series.
2. [ ] Open the chapter review list (`/editor/chapters`).
3. [ ] Confirm the list only shows chapters from series where this editor is an active Tantou Editor contributor.
4. [ ] Click "Review Chapter" to open the chapter detail route.
5. [ ] Detail loads real chapter data (series title, chapter label, status, pages, creator).
6. [ ] Pages table shows page numbers, current versions, and preview links.
7. [ ] Open annotations list shows issue types, comments, authors, and timestamps.
8. [ ] Click "Review in Workspace" — opens `/series/{slug}/workspace?chapterId={chapterId}`.
9. [ ] Login as another Tantou Editor who is not a contributor of that series.
10. [ ] Try opening the same chapter detail URL directly.
11. [ ] API returns 403, UI shows "Access Denied" state with no chapter/series details leaked.
12. [ ] Confirm `AnnotationWorkspace.razor` is not used as the review target.
13. [ ] Confirm `/mangaka/workspace/{SeriesId}` is not restored.
14. [ ] Confirm no direct Web-to-Application/Infrastructure/EF injection remains.
15. [ ] Build succeeds with 0 errors.

## Remaining Tasks
- Manual browser/database verification still recommended (real Tantou Editor login, real chapters with pages and annotations, workspace access guard).
- `AssignedEditorDisplayName` is always null (no editor assignment concept in schema). If editor assignment is added later, update the detail DTO.
- `CurrentVersionNo` shows version number, not timestamp. If a timestamp is needed, add `CreatedAtUtc` to `ChapterPageVersion` entity and update the mapping.
- Annotation write/resolve actions remain out of scope. The "Review in Workspace" button directs editors to the workspace for these operations.
- Editorial decision actions (approve/request revision/cancel) remain out of scope. The workspace handles these.
- Other Editor pages (`AnnotationWorkspace.razor`) still contain mock data and may be deprecated or migrated separately.
