# Editor Pipeline: Filter, Scope, Pagination Follow-up

**Date:** 2026-06-19
**Models:** deepseek-v4-flash-free (sessions 1-2), deepseek-v4-flash-free (session 3+)

## Summary

Completed 6 remaining chunks after the Annotation Workspace real-data migration, plus fixed 3 `MUD0002 RowGutter` warnings.

### Chunk 1 — vw_ActiveSeriesContributor View Mapping
- Created `ActiveSeriesContributor.cs` keyless entity matching `SeriesBoardPollVoteSummary` pattern
- Added `DbSet<ActiveSeriesContributor>` + `ToView("vw_ActiveSeriesContributor", "manga").HasNoKey()` to `ApplicationDbContext`
- Removed private `ScopedSeriesIdsQuery` methods from `EditorAnnotationRepository` and `EditorChapterReviewRepository`
- Both repositories now query the view via `_dbContext.ActiveSeriesContributors`

### Chunk 2 — Issue Type Filter Fix
- Created `Application/Common/Constants/ChapterPageAnnotationIssueTypes.cs` with all 11 valid issue types + a static `All` list
- Updated `GetEditorAnnotationsQueryHandler.IssueTypeFilters` to use the constant list (not data-derived)
- Added safety: invalid issue type values fall back to "all" instead of empty results
- Removed data-derived `issueTypeFilters` from `EditorAnnotationRepository.GetAnnotationsAsync`

### Chunk 3 — MudTable RowsPerPage Pagination
- `ProposalList.razor`: added `RowsPerPage="15"`
- `ChapterReviewList.razor`: added `RowsPerPage="15"`
- `AnnotationWorkspace.razor` (inner MudTable): added `RowsPerPage="10"`

### Chunk 4 — Dashboard Scoping
- Added `actorUserId` to `GetEditorDashboardQuery`, `IEditorDashboardRepository`, and `EditorDashboardRepository`
- `SerializedSeriesCount` now uses subquery against `ActiveSeriesContributors` to scope to editor-contributed series
- `RecentSeriesActivity` now filters through the same `ActiveSeriesContributors` subquery
- Pending proposal/chapter/annotation counts remain global (editor queue)

### Chunk 5 — Series Library Rewrite (CA Fix)
- Created full typed pipeline: `IEditorSeriesRepository` (Domain) → `GetEditorSeriesQuery`/Handler (Application) → `EditorSeriesController` (API) → `IEditorSeriesApiClient`/`EditorSeriesApiClient` (Web)
- Removed direct `ISeriesService` dependency from `SeriesList.razor` (was Clean Architecture violation)
- Series list is now scoped to editor-contributed series via `ActiveSeriesContributors` view
- Added `RowsPerPage="15"` to the series table
- Includes `@using System.Security.Claims` and `AuthenticationStateProvider` for actor user ID resolution

### Chunk 6 — Series Cover on Proposal Detail
- Added `SeriesCoverUrl` (`string?`) to `EditorProposalDetailDto`
- Updated `SeriesProposalRepository.GetByIdWithDetailsAsync` to `Include(sp => sp.Series).ThenInclude(s => s.CoverFile)`
- Handler maps `proposal.Series?.CoverFile?.CloudinarySecureUrl`
- `ProposalReviewDetail.razor` sidebar shows cover image card (conditional on `SeriesCoverUrl` being non-null)

### Pre-fix: 3 MUD0002 RowGutter Warnings
- `AnnotationWorkspace.razor`: replaced `RowGutter` with `Spacing` on 3 `MudStack` elements

## Files Changed / Created

| File | Action |
|------|--------|
| `Domain/Entities/ActiveSeriesContributor.cs` | **Created** — keyless view model |
| `Application/Common/Constants/ChapterPageAnnotationIssueTypes.cs` | **Created** — 11 issue type constants |
| `Application/DTOs/Editor/EditorSeriesDtos.cs` | **Created** — `EditorSeriesDto`, `EditorSeriesListDto` |
| `Application/DTOs/Manga/SeriesProposalDtos.cs` | **Modified** — added `SeriesCoverUrl` to `EditorProposalDetailDto` |
| `Domain/Interfaces/IEditorDashboardRepository.cs` | **Modified** — added `actorUserId` param |
| `Domain/Interfaces/IEditorSeriesRepository.cs` | **Created** |
| `Application/Features/Editor/Dashboard/Queries/GetEditorDashboard/` | **Modified** — query + handler pass `actorUserId` |
| `Application/Features/Editor/Series/Queries/GetEditorSeries/` | **Created** — query + handler |
| `Application/Features/Editor/Annotations/Queries/GetEditorAnnotations/` | **Modified** — uses constant issue type list |
| `Application/Features/Editor/SeriesProposals/Queries/GetEditorProposalDetail/` | **Modified** — maps `SeriesCoverUrl` |
| `Infrastructure/Repositories/EditorDashboardRepository.cs` | **Modified** — scoped counts via `ActiveSeriesContributors` |
| `Infrastructure/Repositories/EditorAnnotationRepository.cs` | **Modified** — uses `ActiveSeriesContributors`, removed data-derived filters |
| `Infrastructure/Repositories/EditorChapterReviewRepository.cs` | **Modified** — uses `ActiveSeriesContributors` |
| `Infrastructure/Repositories/EditorSeriesRepository.cs` | **Created** |
| `Infrastructure/Repositories/SeriesProposalRepository.cs` | **Modified** — `ThenInclude(s => s.CoverFile)` |
| `Infrastructure/DependencyInjection.cs` | **Modified** — registers `IEditorSeriesRepository` |
| `API/Controllers/Editor/EditorDashboardController.cs` | **Modified** — passes `actorUserId` to query |
| `API/Controllers/Editor/EditorSeriesController.cs` | **Created** |
| `Web/Program.cs` | **Modified** — registers `IEditorSeriesApiClient` |
| `Web/Services/Api/IEditorSeriesApiClient.cs` | **Created** |
| `Web/Services/Api/EditorSeriesApiClient.cs` | **Created** |
| `Web/Components/Pages/Editor/AnnotationWorkspace.razor` | **Modified** — `RowGutter→Spacing`, `RowsPerPage` |
| `Web/Components/Pages/Editor/ProposalList.razor` | **Modified** — `RowsPerPage` |
| `Web/Components/Pages/Editor/ChapterReviewList.razor` | **Modified** — `RowsPerPage` |
| `Web/Components/Pages/Editor/SeriesList.razor` | **Rewritten** — typed API client, scoped list |
| `Web/Components/Pages/Editor/ProposalReviewDetail.razor` | **Modified** — series cover card in sidebar |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | **Modified** — `ActiveSeriesContributors` DbSet + view mapping |

## Build Result
- `dotnet build --no-incremental`: 60-61 warnings (all pre-existing), 0 errors
- No new warnings from changed files

## Key Decisions
- Dashboard scoping uses correlated subquery (`Any`) against `ActiveSeriesContributors` view, consistent with other repositories
- `EditorSeriesRepository` returns all `Series` entities (no pagination) — client-side `RowsPerPage` on MudTable handles display
- Series cover is purely optional — no placeholder icon in sidebar, just omitted when null
- Controller uses `X-Actor-User-Id` header pattern consistent with all other Editor controllers
