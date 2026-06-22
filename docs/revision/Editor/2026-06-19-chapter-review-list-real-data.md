# Chapter Review List Real Data

## Branch

`feature/Mangaka`

## Date

2026-06-19

## Task Summary

Replaced all hard-coded mock data on the Tantou Editor Chapter Review List (`/editor/chapters`)
with real database values served through the project's canonical architecture path:

```
Blazor Web -> typed API client -> API controller -> MediatR query
-> Application handler -> Infrastructure EF AsNoTracking read queries -> SQL Server
```

The previous page rendered 5 static chapter rows (Crimson Moon Academy, Neon Samurai Path,
Lunar Eclipse Academy, Shadow Garden Tales) and hard-coded KPI values (5/12/2/1). The page
now loads real chapters from the database, with loading, error+retry, and empty states.
The "Review Chapter" button navigates to the workspace at
`/series/{slug}/workspace?chapterId={chapterId}`.

## Architecture Path

```
ChapterReviewList.razor
  -> IEditorChapterReviewApiClient.GetReviewQueueAsync(actorUserId, statusFilter)
  -> GET /api/editor/chapters/review-queue?status=...
  -> EditorChapterReviewsController.GetReviewQueueAsync
  -> IMediator.Send(GetEditorChapterReviewQueueQuery)
  -> GetEditorChapterReviewQueueQueryHandler
  -> IEditorChapterReviewRepository.GetReviewQueueAsync (EF Core AsNoTracking)
  -> SQL Server
```

No direct Web-to-Application, Web-to-Infrastructure, Web-to-EF, or Web-to-stored-procedure
calls remain on this page.

## Mock Data Removed

From `ChapterReviewList.razor`:

- KPI fields: `_underReviewCount = 5`, `_approvedCount = 12`, `_revisionCount = 2`,
  `_onHoldCount = 1`.
- Hard-coded `_chapters` list with 5 rows:
  - `Crimson Moon Academy` Ch. 45 "The Shadow Rises" (24 pages, UNDER_REVIEW)
  - `Crimson Moon Academy` Ch. 44 "Before the Dawn" (22 pages, UNDER_REVIEW)
  - `Neon Samurai Path` Ch. 19 "Blade of Light" (20 pages, UNDER_REVIEW)
  - `Lunar Eclipse Academy` Ch. 13 "New Moon Festival" (18 pages, REVISION_REQUESTED)
  - `Shadow Garden Tales` Ch. 9 (16 pages, ON_HOLD)
- Local `ChapterRow` record type.
- `OpenChapter(Guid chapterId)` navigating to `/editor/chapters/{chapterId}` (old mock route).

## Files Changed

### Added

| File | Purpose |
|------|---------|
| `src/MangaManagementSystem.Application/DTOs/Editor/EditorChapterReviewDtos.cs` | `EditorChapterReviewQueueDto` (KPI counts + chapter list) and `EditorChapterReviewQueueItemDto` (series title, slug, chapter label/title, status, page count, workspace URL) |
| `src/MangaManagementSystem.Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewQueue/GetEditorChapterReviewQueueQuery.cs` | MediatR query record (optional status filter) |
| `src/MangaManagementSystem.Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewQueue/GetEditorChapterReviewQueueQueryHandler.cs` | Maps Domain `EditorChapterReviewChapter` records to DTOs; constructs workspace URLs |
| `src/MangaManagementSystem.Domain/Interfaces/IEditorChapterReviewRepository.cs` | Read-only repository interface + `EditorChapterReviewData` and `EditorChapterReviewChapter` Domain records |
| `src/MangaManagementSystem.Infrastructure/Repositories/EditorChapterReviewRepository.cs` | EF Core `AsNoTracking` implementation with server-side KPI counts and correlated page-count subquery |
| `src/MangaManagementSystem.API/Controllers/Editor/EditorChapterReviewsController.cs` | Thin controller: `X-Actor-User-Id` header + `IMediator.Send` |
| `src/MangaManagementSystem.Web/Services/Api/IEditorChapterReviewApiClient.cs` | Typed client interface |
| `src/MangaManagementSystem.Web/Services/Api/EditorChapterReviewApiClient.cs` | HttpClient implementation with safe error parsing |

### Modified

| File | Change |
|------|--------|
| `src/MangaManagementSystem.Web/Components/Pages/Editor/ChapterReviewList.razor` | Removed all mock data; loads `EditorChapterReviewQueueDto` via typed client; loading/error+retry/empty states; "Review Chapter" button navigates to `/series/{slug}/workspace?chapterId={id}`; status filter with Refresh button |
| `src/MangaManagementSystem.Web/Program.cs` | Registered `IEditorChapterReviewApiClient` / `EditorChapterReviewApiClient` via `AddHttpClient` |
| `src/MangaManagementSystem.Infrastructure/DependencyInjection.cs` | Registered `IEditorChapterReviewRepository` / `EditorChapterReviewRepository` |

## Endpoint Added

- `GET /api/editor/chapters/review-queue?status=UNDER_REVIEW` — returns
  `EditorChapterReviewQueueDto`. Requires the transitional `X-Actor-User-Id` header.
  Returns HTTP 400 if the actor header is missing/invalid, HTTP 500 on unexpected error.

## Query / Repository Methods Added

- **Query**: `GetEditorChapterReviewQueueQuery(string? StatusFilter)` →
  `GetEditorChapterReviewQueueQueryHandler` — maps Domain records to DTOs; constructs
  `WorkspaceUrl = /series/{slug}/workspace?chapterId={id}` for each chapter.
- **Repository**: `IEditorChapterReviewRepository.GetReviewQueueAsync(string? statusFilter,
  CancellationToken)` — returns `EditorChapterReviewData` with:
  - 4 KPI counts (Under Review, Approved This Week, Revision Requested, On Hold)
  - Filtered chapter list with correlated `ChapterPage` count subquery (excludes soft-deleted
    pages via `DeletedAtUtc == null`)
  - Parent `Series` eagerly loaded for title/slug

## Data Mapping

| Element | Source | Rule |
|---------|--------|------|
| Under Review | `Chapter` | Count where `StatusCode == "UNDER_REVIEW"` (global MVP) |
| Approved This Week | `Chapter` | Count where `StatusCode == "APPROVED"` and `CreatedAtUtc >= 7 days ago` |
| Revision Requested | `Chapter` | Count where `StatusCode == "REVISION_REQUESTED"` (global MVP) |
| On Hold | `Chapter` | Count where `StatusCode == "ON_HOLD"` (global MVP) |
| Chapter list | `Chapter` + `Series` | Filtered by status (default: all reviewable); ordered by `CreatedAtUtc` desc |
| Page count | `ChapterPage` | Correlated subquery: `Count(cp where cp.ChapterId == c.ChapterId && cp.DeletedAtUtc == null)` |

**Actor scoping**: Global MVP. The current schema has no per-Tantou-Editor contributor
relationship for chapter review assignments. This matches the documented Editor Dashboard
decision.

**Timestamp**: Chapter has no `SubmittedAtUtc` or `UpdatedAtUtc`; `CreatedAtUtc` is used as
the best available ordering/display timestamp. "Approved This Week" also uses `CreatedAtUtc`
as the closest available proxy for when the chapter reached APPROVED status.

## Workspace Redirect Behavior

- Workspace URL is constructed server-side in the Application handler:
  `/series/{seriesSlug}/workspace?chapterId={chapterId}`
- The "Review Chapter" button uses `NavigationManager.NavigateTo(workspaceUrl)`.
- If `SeriesSlug` is null/empty, the button is rendered disabled.
- The old mock route `/editor/chapters/{chapterId}` has been removed.
- No old `/mangaka/workspace/{SeriesId}` route is used or restored.

## UI Behavior

- Chapter review queue loads through `IEditorChapterReviewApiClient` only.
- **Loading state**: indeterminate progress bar while fetching.
- **Error state**: red alert with failure message and Retry button.
- **Empty state**: "No Chapters Pending Review" when the filtered list is empty.
- **Status filter**: dropdown (All / Under Review / Revision Requested / On Hold) with Refresh
  button. Filter change triggers a re-fetch.
- **KPI cards**: 4 real counts from the database (Under Review, Approved This Week,
  Revision Requested, On Hold).
- Status chips display with underscores replaced by spaces.
- Page count shown as `{n} pages`.
- No mock series/chapter names remain on the page.

## Build Result

```
dotnet build MangaManagementSystem\MangaManagementSystem.sln
```

- **Build succeeded**
- **0 errors**
- **60 warnings**

The 60 warnings are all pre-existing categories (`MUD002`, `CS0649`, `CS0108`, `CS8602`,
`CS8604`, `CS8981`, `CS0618`, `CS0105`, `RZ10012`). A filtered build check for
chapter-review-related files produced no output, confirming **no new warnings were
introduced by this task**.

## Manual Test Checklist

1. [ ] Login as active Tantou Editor.
2. [ ] Open the chapter review list page (`/editor/chapters`).
3. [ ] Confirm no mock chapter/series rows remain.
4. [ ] Queue loads real `UNDER_REVIEW` / `REVISION_REQUESTED` / `ON_HOLD` chapters from the database.
5. [ ] Empty state appears when no chapters match.
6. [ ] Loading state appears during fetch.
7. [ ] Error state and Retry work if API fails.
8. [ ] "Review Chapter" navigates to `/series/{slug}/workspace?chapterId={chapterId}`.
9. [ ] Workspace opens for the selected series/chapter when the actor has access.
10. [ ] No old `/mangaka/workspace/{SeriesId}` route is restored.
11. [ ] No direct Web-to-Application/Infrastructure/EF injection remains.
12. [ ] Build succeeds with 0 errors.

## Remaining Tasks

- Manual browser/database verification still recommended (real Tantou Editor login, real
  chapters with pages, workspace access guard).
- **Per-editor scoping**: chapter review queue is currently a global MVP queue. If a Tantou
  Editor contributor scope is later defined for chapter review assignments, the repository
  query should be narrowed.
- **Approved This Week** uses `CreatedAtUtc` as a proxy for approval timestamp. If an
  `ApprovedAtUtc` or a chapter-status audit trail is added later, this should be refined.
- **Version count**: the DTO does not include page-version counts. The
  `ChapterPageVersion.IsCurrentVersion` field exists, but deriving a version count per
  chapter requires a two-level join. Documented as future enhancement if needed.
- Other Editor pages (`ChapterReviewDetail.razor`, `AnnotationWorkspace.razor`) and other
  roles' pages still contain mock data and remain future work.
