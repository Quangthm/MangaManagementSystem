# Editor Annotation Workspace — Real Data (Read-Only, Scoped)

**Branch:** `feature/Mangaka`
**Date:** 2026-06-19
**Task:** Replace Tantou Editor Annotation Workspace mock data with real DB/API data scoped to the current editor.

---

## Architecture Path

```
Blazor Web (AnnotationWorkspace.razor)
  → IEditorAnnotationApiClient / EditorAnnotationApiClient (typed HttpClient)
    → EditorAnnotationsController (API, thin controller)
      → MediatR: GetEditorAnnotationsQuery → GetEditorAnnotationsQueryHandler
        → IEditorAnnotationRepository / EditorAnnotationRepository (EF AsNoTracking)
          → SQL Server (ChapterPageAnnotation, ChapterPageAnnotationPageRegion, etc.)
```

No direct Web-to-Application/Infrastructure/EF calls. All data flows through typed API client → controller → MediatR → handler → repository.

---

## Scope Rule

- Server-side: `AnnotatedByUserId == actorUserId` AND current user is an active Tantou Editor contributor of the annotation's series.
- Filtering is applied in `EditorAnnotationRepository` via joins to `SeriesEditorContributor` and the `ScopedSeriesIdsQuery` pattern.

---

## Mock Data Removed

All previously hardcoded annotations (Crimson Moon, Neon Samurai, Nightfall Chronicles, etc.) removed from `AnnotationWorkspace.razor`. The component now loads data exclusively through `IEditorAnnotationApiClient`.

---

## Files Changed

| Layer | File | Action |
|-------|------|--------|
| Domain | `Domain/Interfaces/IEditorAnnotationRepository.cs` | Created |
| Application | `Application/DTOs/Editor/EditorAnnotationDtos.cs` | Created |
| Application | `Application/Features/Editor/Annotations/Queries/GetEditorAnnotations/GetEditorAnnotationsQuery.cs` | Created |
| Application | `Application/Features/Editor/Annotations/Queries/GetEditorAnnotations/GetEditorAnnotationsQueryHandler.cs` | Created |
| Infrastructure | `Infrastructure/Repositories/EditorAnnotationRepository.cs` | Created |
| Infrastructure | `Infrastructure/DependencyInjection.cs` | Updated (DI registration) |
| API | `API/Controllers/Editor/EditorAnnotationsController.cs` | Created |
| Web | `Web/Services/Api/IEditorAnnotationApiClient.cs` | Created |
| Web | `Web/Services/Api/EditorAnnotationApiClient.cs` | Created |
| Web | `Web/Program.cs` | Updated (HttpClient registration) |
| Web | `Web/Components/Pages/Editor/AnnotationWorkspace.razor` | Rewritten (no mock data) |
| Web | `Web/Components/Pages/Mangaka/CreatorWorkspace.razor` | Updated (allowlist for return URL) |

---

## Endpoint Added

`GET /api/editor/annotations?actorUserId={userId}&seriesId={seriesId}&issueType={issueType}&status={status}`

Returns `EditorAnnotationWorkspaceDto`.

---

## Query / Repository Methods Added

- `GetEditorAnnotationsQuery` + `GetEditorAnnotationsQueryHandler`: MediatR query/handler
- `IEditorAnnotationRepository.GetEditorAnnotationsAsync()`: Domain interface method
- `EditorAnnotationRepository.GetEditorAnnotationsAsync()`: EF implementation with AsNoTracking, contributor scoping, regions join, series grouping, filter parameters

---

## Data Mapping

```
ChapterPageAnnotation → EditorAnnotationDto:
  - Id, ChapterId, ChapterNumberLabel, ChapterTitle, PageNumber
  - VersionNo (nullable int)
  - IssueTypeCode (string)
  - AnnotationText (string)
  - IsResolved (bool)
  - Regions → List<EditorAnnotationRegionDto>
  - Slug (for workspace URL construction)
  - SeriesTitle (from join)

ChapterPageAnnotationPageRegion → EditorAnnotationRegionDto:
  - RegionTypeCode (string)
  - X, Y (coordinates, nullable double)

Series-level → EditorAnnotationSeriesGroupDto:
  - SeriesId, SeriesTitle
  - Annotations (list)

Top-level → EditorAnnotationWorkspaceDto:
  - OpenCount, ResolvedCount, PagesWithIssuesCount
  - DistinctIssueTypeCount (replaces "Critical")
  - SeriesFilters → EditorAnnotationSeriesFilterDto (SeriesId, SeriesTitle)
  - IssueTypeFilters (List<string>)
  - SeriesGroups (list of grouped annotations)
```

---

## KPI Mapping

| Card | Data Source | Notes |
|------|-------------|-------|
| Open | `annotations.Count(a => !a.IsResolved)` | |
| Resolved | `annotations.Count(a => a.IsResolved)` | |
| Pages With Issues | Distinct `ChapterId + PageNumber` across annotations | |
| Issue Types | Distinct `IssueTypeCode` count | Replaced "Critical" because no severity/critical field exists in schema |

---

## Filter Behavior

- **Series filter** (`MudSelect`): Lists only scoped/contributed series where the current editor is an active Tantou Editor contributor. "All Series" default. Triggers `ApplyFiltersAsync`.
- **Issue type filter** (`MudSelect`): Lists distinct `IssueTypeCode` values from the full data set. "All Issues" default.
- **Status filter** (`MudSelect`): "All", "Open", "Resolved". Defaults to "All".
- **Refresh button**: Calls `LoadDataAsync()` with current filter values passed as query params to the API.

## Grouping Behavior

- Annotations grouped by series using `MudExpansionPanels` with `MultiExpansion="true"`.
- Each `MudExpansionPanel` header shows series title + annotation count chip.
- Inside each panel, a `MudTable` lists annotations with columns: Chapter/Page, Version, Issue Type, Text, Status, Regions, Actions.
- Same region linked to multiple annotations appears under each relevant annotation.

## Workspace Redirect Behavior

- "Open Page" button navigates to `/series/{slug}/workspace?chapterId={chapterId}&returnUrl=%2Feditor%2Fannotations` via `Nav.NavigateTo()`.
- If `slug` is missing/empty, the button is disabled with a "—" fallback.
- `CreatorWorkspace.razor` has `/editor/annotations` added to `IsSafeLocalReturnUrl` allowlist for proper back-navigation.

---

## Build Result

**Final:** Build succeeded — **0 errors, 60 warnings** (matches audited baseline exactly).

### Warning Fix: 3 MUD0002 RowGutter

Before completion, the build emitted 3 new `MUD0002` warnings from `AnnotationWorkspace.razor`:

| Line | Deprecated Attribute | Fixed To |
|------|---------------------|----------|
| 69 | `RowGutter="16"` | `Spacing="4"` |
| 72 | `RowGutter="8"` | `Spacing="2"` |
| 168 | `RowGutter="4"` | `Spacing="1"` |

All 3 warnings eliminated. The file now produces zero warnings.

### Warning Comparison vs 60-Warning Baseline

| Warning Code | Count | Files | Status |
|-------------|-------|-------|--------|
| CS8981 | 4 | `common.cs`, `enums.cs`, `features.cs` | Baseline |
| CS8618 | 1 | `DisplayNameUpdateDto.cs` | Baseline |
| CS8604 | 2 | `AuditEventService.cs`, `ChapterPageAnnotationService.cs` | Baseline |
| CS0108 | 4 | `ChapterPageTaskRepository.cs`, `ChapterPageAnnotationRepository.cs`, `SeriesRepository.cs`, `UserRepository.cs` | Baseline |
| CS0618 | 2 | `ChapterEditorialReviewConfiguration.cs`, `AuditEventConfiguration.cs` | Baseline |
| CS8602 | 10 | `ChapterPageTaskRepository.cs` (all) | Baseline |
| CS8603 | 1 | `CloudinaryFileStorageFormAdapter.cs` | Baseline |
| CS0105 | 2 | `API/Program.cs` | Baseline |
| RZ10012 | 12 | `StudioWorkspace.razor` (6), `TaskDetail.razor` (6) | Baseline |
| CS0649 | 2 | `AssistantDashboard.razor`, `BoardPolls.razor` | Baseline |
| MUD0002 | 20 | `SystemSettings.razor` (6), `UserAvatarMenu.razor` (1), `BoardPolls.razor` (3), `StudioWorkspace.razor` (2), `TaskDetail.razor` (2), `UserApproval.razor` (1), `LandingPage.razor` (1), `CreatorWorkspace.razor` (4) | Baseline |
| **Total** | **60** | | **No new warnings** |

---

## Manual Test Checklist

1. Login as active Tantou Editor contributor.
2. Open `/editor/annotations`.
3. Confirm no Crimson Moon / Neon Samurai / fake annotation rows remain.
4. Page loads only annotations authored by the current editor.
5. Page only includes series where current editor is active Tantou Editor contributor.
6. Series filter only shows scoped/contributed series.
7. Issue type filter works.
8. Status filter works for open/resolved/all.
9. Annotation with multiple linked regions shows regions grouped under that annotation.
10. Same region linked to multiple annotations appears under each relevant annotation group.
11. Open Page navigates to `/series/{slug}/workspace?chapterId={chapterId}&returnUrl=%2Feditor%2Fannotations`.
12. Workspace back arrow returns to `/editor/annotations`.
13. Missing slug disables or hides Open Page safely.
14. Empty state works when no annotations match.
15. Error + retry state works.
16. No direct Web-to-Application/Infrastructure/EF injection exists.
17. Build succeeds with 0 errors.
18. Changed files introduce 0 new warnings.

---

## Remaining Tasks

- [ ] Run manual test checklist above against live DB.
- [ ] Phase 2 Snyk findings (PII exposure, auth-related log forging, SSRF in Program.cs, ProfileController redesign) — deferred for auth teammate and design discussion.
