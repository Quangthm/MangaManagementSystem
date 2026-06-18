# Series Detail Page and Workspace Route Migration

**Date:** 2026-06-18  
**Branch:** feature/Mangaka  
**Author:** Claude (AI Assistant)

## Summary

Replaced the minimal `/series/{slug}` stub with a full series detail page and migrated the workspace route from `/mangaka/workspace/{SeriesId}` to `/series/{slug}/workspace`. This establishes the canonical workspace URL pattern and provides proper series browsing with chapter listing.

## Changes

### 1. Series Detail Page (`/series/{slug}`)

**File:** `src/MangaManagementSystem.Web/Components/Pages/Series/SeriesPage.razor`

Replaced stub with full implementation:
- Cover image display (220px width)
- Series metadata: title, status badge, genre, language, publication frequency
- Contributor names (all active contributors)
- Synopsis
- Open Workspace button (enabled/disabled based on access check)
- Chapter list with pagination (10 chapters per page)
- Chapter rows link to `/series/{slug}/workspace?chapterId={chapterId}`

**Architecture:**
```
SeriesPage.razor
  → ISeriesApiClient.GetSeriesDetailAsync(slug, chapterPage, chapterPageSize)
  → GET /api/series/{slug}?chapterPage=1&chapterPageSize=10
  → SeriesController.GetBySlugAsync()
  → GetSeriesBySlugQuery
  → ISeriesRepository.GetSeriesDetailBySlugAsync()
  → EF Core read query
```

### 2. Workspace Route Migration

**File:** `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor`

**Route change:**
- **Removed:** `@page "/mangaka/workspace/{SeriesId}"`
- **Added:** `@page "/series/{slug}/workspace"`

**Parameter change:**
- **Removed:** `[Parameter] public string? SeriesId { get; set; }`
- **Added:** `[Parameter] public string slug { get; set; } = string.Empty;`
- **Added:** `[SupplyParameterFromQuery] public string? chapterId { get; set; }`

**Authorization change:**
- **Before:** `[Authorize(Roles = "Mangaka")]`
- **After:** `[Authorize(Roles = "Mangaka,Tantou Editor,Assistant")]`

**Workspace access guard:**
- Added series-specific access check via `ISeriesApiClient.GetWorkspaceEntryAsync(actorUserId, slug)`
- Resolves `slug → SeriesId` internally
- Returns `SeriesWorkspaceEntryDto` with `CanAccess` flag
- If `CanAccess == false`, shows access denied UI instead of loading workspace

**Back button:**
- Updated from `Href="/mangaka"` to `Href="@($"/series/{slug}")"`

**Architecture:**
```
CreatorWorkspace.razor
  → ISeriesApiClient.GetWorkspaceEntryAsync(actorUserId, slug)
  → GET /api/series/{slug}/workspace-entry
  → SeriesController.GetWorkspaceEntryAsync()
  → GetSeriesWorkspaceEntryQuery
  → ISeriesRepository.GetWorkspaceEntryBySlugAsync()
  → EF Core read query
```

### 3. Dashboard Task List Cleanup

**File:** `src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor`

**Removed:** "Open in Workspace" button from task list (line 320)

**Reason:** Task data has `SeriesId` but no `slug`. Adding slug to task DTO would require touching task service logic, which is outside the scope of this task.

**Status:** Documented as pending slug-based routing support for task shortcuts.

### 4. Backend Implementation

**New Files:**

1. **DTOs** (`src/MangaManagementSystem.Application/DTOs/Manga/SeriesDetailDtos.cs`):
   - `SeriesDetailDto` - Series metadata + contributor names + paginated chapters
   - `SeriesChapterListItemDto` - Chapter summary for list display
   - `SeriesWorkspaceEntryDto` - Workspace access check result

2. **Repository Methods** (`src/MangaManagementSystem.Domain/Interfaces/ISeriesRepository.cs`):
   - `GetSeriesDetailBySlugAsync(string slug, int chapterPage, int chapterPageSize)` - Returns series with contributors and paginated chapters
   - `GetWorkspaceEntryBySlugAsync(string slug, Guid actorUserId)` - Returns workspace access decision

3. **Repository Implementation** (`src/MangaManagementSystem.Infrastructure/Repositories/SeriesRepository.cs`):
   - Implemented both methods using EF Core read queries
   - Contributor query: `SeriesContributor.UserId == actorUserId && EndDate == null`
   - Workspace access check: validates actor is active contributor with allowed role

4. **MediatR Queries**:
   - `GetSeriesBySlugQuery` + handler
   - `GetSeriesWorkspaceEntryQuery` + handler

5. **API Controller** (`src/MangaManagementSystem.API/Controllers/SeriesController.cs`):
   - `GET /api/series/{slug}` - Returns series detail with chapters
   - `GET /api/series/{slug}/workspace-entry` - Returns workspace access decision

6. **Typed API Client** (`src/MangaManagementSystem.Web/Services/Api/`):
   - `ISeriesApiClient` interface
   - `SeriesApiClient` implementation
   - Registered in `Program.cs`

## Workspace Access Guard

**Rule:**
```
actor account status == ACTIVE
AND actor role ∈ {Mangaka, Tantou Editor, Assistant}
AND Series.Slug == slug
AND SeriesContributor.UserId == actorUserId
AND SeriesContributor.EndDate IS NULL
```

**Implementation:**
- Checked in `CreatorWorkspace.razor` via `GetWorkspaceEntryAsync()`
- Returns `SeriesWorkspaceEntryDto` with `CanAccess` flag
- If `CanAccess == false`, shows access denied UI with "Back to Series" button

**Note:** Current `SeriesContributor` model does not distinguish assistant task assignment from contributor membership. Assistant access is granted only if they have an active `SeriesContributor` row. Future task-based assistant access is pending.

## Chapter Pagination

**Default:** 10 chapters per page  
**Min page:** 1  
**Min size:** 1  
**Max size:** 50  
**Total pages:** `ceiling(totalCount / pageSize)`

**Sort:** By `ChapterNumberLabel` ascending

**UI:** Pagination controls below chapter list with Prev/Next buttons and page numbers.

## Chapter Navigation

**Click behavior:** Each chapter row navigates to `/series/{slug}/workspace?chapterId={chapterId}`

**Query parameter:** Uses real database `ChapterId` (GUID)

**Auto-selection:** Pending workspace refactor. The `chapterId` parameter is preserved in the URL but workspace does not currently auto-select the chapter.

## Build Result

```
Build succeeded.
    0 Error(s)
```

## Manual Test Checklist

- [ ] Navigate to `/series/{slug}` for a serialized series
- [ ] Verify cover image displays correctly
- [ ] Verify all metadata displays: title, status, genre, language, frequency, contributors, synopsis
- [ ] Verify chapter list shows with pagination
- [ ] Click chapter row → verify navigation to `/series/{slug}/workspace?chapterId={id}`
- [ ] Verify "Open Workspace" button is enabled for active contributors
- [ ] Verify "Open Workspace" button is disabled for non-contributors
- [ ] Navigate to `/series/{slug}/workspace` as active contributor → verify workspace loads
- [ ] Navigate to `/series/{slug}/workspace` as non-contributor → verify access denied UI
- [ ] Verify back button in workspace returns to `/series/{slug}`
- [ ] Verify old route `/mangaka/workspace/{SeriesId}` returns 404
- [ ] Verify pagination works: click page 2, verify chapters update
- [ ] Verify chapter count displays correctly in header

## Remaining Tasks

1. **Task list workspace shortcut:** Add slug to task DTO and re-enable "Open in Workspace" button in Mangaka dashboard task list.

2. **Workspace chapter auto-selection:** Implement logic in `CreatorWorkspace.razor` to read `chapterId` query parameter and auto-select the chapter.

3. **Assistant task-based access:** Current workspace access requires `SeriesContributor` row. Future enhancement: allow assistants with assigned tasks to access workspace without explicit contributor row.

4. **Public reader access:** Current `/series/{slug}` page requires authentication. Future enhancement: allow public access to serialized series for reader catalog.

## Files Changed

**Modified:**
- `src/MangaManagementSystem.Web/Components/Pages/Series/SeriesPage.razor`
- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor`
- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor`
- `src/MangaManagementSystem.Domain/Interfaces/ISeriesRepository.cs`
- `src/MangaManagementSystem.Infrastructure/Repositories/SeriesRepository.cs`
- `src/MangaManagementSystem.Web/Program.cs`

**Created:**
- `src/MangaManagementSystem.Application/DTOs/Manga/SeriesDetailDtos.cs`
- `src/MangaManagementSystem.Application/Features/Series/Queries/GetSeriesBySlug/GetSeriesBySlugQuery.cs`
- `src/MangaManagementSystem.Application/Features/Series/Queries/GetSeriesBySlug/GetSeriesBySlugQueryHandler.cs`
- `src/MangaManagementSystem.Application/Features/Series/Queries/GetSeriesWorkspaceEntry/GetSeriesWorkspaceEntryQuery.cs`
- `src/MangaManagementSystem.Application/Features/Series/Queries/GetSeriesWorkspaceEntry/GetSeriesWorkspaceEntryQueryHandler.cs`
- `src/MangaManagementSystem.API/Controllers/SeriesController.cs`
- `src/MangaManagementSystem.Web/Services/Api/ISeriesApiClient.cs`
- `src/MangaManagementSystem.Web/Services/Api/SeriesApiClient.cs`

## Architecture Compliance

✅ Web → API client → Controller → MediatR → Repository → EF Core  
✅ No direct service injection in Razor pages  
✅ No raw SQL in controllers  
✅ No stack traces or internal errors exposed to UI  
✅ Actor ID resolved from auth state, not UI input  
✅ Series-specific access guard enforced server-side  
✅ Role authorization uses confirmed role names: `Mangaka`, `Tantou Editor`, `Assistant`
