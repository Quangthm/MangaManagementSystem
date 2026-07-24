# Editor Workspace Loading JWT Regression Fix

Date: 2026-07-23  
Branch: `feature/Mangaka`

## Symptom and root cause

Creator Workspace remained partially loaded for a Tantou Editor. The first proven failure was:

`OnInitializedAsync` → `MangakaChapterApi.GetSeriesChaptersAsync` → `GET /api/mangaka/series/{seriesId}/chapters` → controller-level Mangaka-only authorization → `403`.

The page, version, file, region, task-context, and annotation controllers had the same controller-level boundary despite containing shared reads. Initialization did not catch downstream failures, so an exception could leave the UI looking permanently unfinished.

## Current initialization trace

| Order | Workspace operation | Client and route | API action | Roles after fix |
|---:|---|---|---|---|
| 1 | Resolve cookie identity, user ID, role | Authentication state | Web boundary | M / E / A |
| 2 | Deferred series access decision | `SeriesApiClient.GetWorkspaceEntryAsync`; `GET api/series/{slug}/workspace-entry` | `SeriesController.GetWorkspaceEntryAsync` | unchanged transitional path |
| 3 | Assistant task context, when supplied | dedicated Assistant client | Assistant task-scoped API | A, task-scoped |
| 4 | Assignment contributor list | `MangakaContributorApi.GetContributorsAsync` | contributor management | M only; call skipped for E/A |
| 5 | Series detail | `SeriesApiClient.GetSeriesDetailAsync` | series detail | existing behavior |
| 6 | Chapter navigation | `MangakaChapterApi.GetSeriesChaptersAsync`; `GET api/mangaka/series/{seriesId}/chapters` | `GetSeriesChaptersAsync` | M / E / A |
| 7 | Page counts | `MangakaPageApi.GetCountsAsync`; `POST api/mangaka/pages/counts` | `GetCountsAsync` | M / E / A |
| 8 | Page list | `MangakaPageApi.GetByChapterAsync`; `GET api/mangaka/pages/by-chapter/{chapterId}` | `GetByChapterAsync` | M / E / A |
| 9 | Version list | `MangakaPageApi.GetVersionsByPageIdsAsync`; `POST api/mangaka/pages/versions/by-page-ids` | `GetVersionsByPageIdsAsync` | M / E / A |
| 10 | Rendering files | `MangakaPageApi.GetFileResourcesByIdsAsync`; `POST api/mangaka/pages/files/by-ids` | `GetFileResourcesByIdsAsync` | M / E / A |
| 11 | Region counts/list | `MangakaRegionApi.GetCountsAsync` / `GetByVersionsAsync` | `POST api/mangaka/regions/counts` / `by-versions` | M / E / A |
| 12 | Page task context | `MangakaTaskApi.GetTasksByPageAsync`; `GET api/mangaka/tasks/by-page/{pageId}` | `GetTasksByPageAsync` | M / E |
| 13 | Page annotations | `MangakaAnnotationApi.GetByPageAsync`; `GET api/mangaka/annotations/by-page/{pageId}` | `GetByPageAsync` | M / E |
| 14 | Initial page render | local canvas load | none | M / E / A |

Assistant skips the general task/annotation calls and retains its dedicated task-scoped context.

## Actor and resource authorization

`IAuthenticatedActorResolver` gained a backwards-compatible `params string[] allowedRoles` overload. Existing single-role calls remain valid. Both paths reload the database account, require `ACTIVE`, and compare the current database role. Failure mapping remains invalid identity/missing user = 401; inactive/wrong role = 403.

`IWorkspaceResourceAuthorizationService` and its EF implementation provide a focused resource-to-series guard. It resolves chapter, page, version, rendering file, region, and annotation IDs to one owning series and requires an active `SeriesContributor` row for the JWT actor. Every supplied batch ID must resolve; mixed-series and inaccessible batches fail. Assistant access is series/workspace-scoped and deliberately is not limited to an assigned-task page.

No schema, migration, stored procedure, or SQL changes were made.

## Changed action matrix

| Controller | Action | Mangaka | Tantou Editor | Assistant | Resource guard | Reason |
|---|---|:---:|:---:|:---:|---|---|
| Chapters | `GetSeriesChaptersAsync` | Yes | Yes | Yes | series + existing repository contributor filter | navigation |
| Page | `GetByChapterAsync` | Yes | Yes | Yes | chapter → series | page list |
| Page | `GetByIdAsync` | Yes | Yes | Yes | page → series | page detail |
| Page | `GetCountsAsync` | Yes | Yes | Yes | every chapter, one series | navigation counts |
| Page | `GetVersionsByPageIdsAsync` | Yes | Yes | Yes | every page, one series | version rendering |
| Page | `GetVersionByIdAsync` | Yes | Yes | Yes | version → series | version detail |
| Page | `GetFileResourcesByIdsAsync` | Yes | Yes | Yes | every file through page version, one series | image rendering |
| Region | `GetByVersionsAsync` | Yes | Yes | Yes | every version, one series | overlays |
| Region | `GetCountsAsync` | Yes | Yes | Yes | every version, one series | overlay counts |
| Region | `CreateAsync` | Yes | Yes | No | version → series | annotation target |
| Region | `EnsureFullPageRegionAsync` | Yes | Yes | No | version → series | full-page annotation target |
| Task | `GetTasksByPageAsync` | Yes | Yes | No | page → series | Editor production context |
| Annotation | `GetByPageAsync` | Yes | Yes | No | page → series | editorial context |
| Annotation | `CreateAsync` | Yes | Yes | No | every region, one series | editorial annotation |
| Annotation | `ResolveAsync` | Yes | Yes | No | annotation regions → series | existing resolution rules |

All chapter/page/version production mutations remain explicit Mangaka-only actions. Region bulk replacement remains Mangaka-only. All general task-management actions remain Mangaka-only. Assistant receives no region, annotation, chapter, page, version, or task-management mutation permission.

Editor region creation passes the JWT-derived actor to the existing `CreatedByUserId` flow. Editor annotation creation passes the same actor as `AnnotatedByUserId`. Existing persistence and audit behavior is unchanged.

## Assistant clarification

Once an active Assistant is an active contributor of Series X, the Assistant can navigate/read shared chapter/page/version/file/region resources anywhere within Series X. Reads for unrelated Series Y fail the resource guard. Assigned-task identity remains authoritative for Assistant task submission, task state changes, completed-work behavior, and other dedicated task operations only.

## Loading reliability

Creator Workspace now has an explicit initialization spinner and downstream load-error panel with Retry and Back actions. Downstream initialization is protected by `catch` plus `finally`, so the initialization flag always clears. Workspace-entry denial retains the existing access-denied view. Retry reloads the current workspace route.

## Files changed

- `MangaManagementSystem/src/MangaManagementSystem.API/Security/AuthenticatedActorResolver.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaChaptersController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaPageController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaPageRegionController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaTaskController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaAnnotationController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Interfaces/IWorkspaceResourceAuthorizationService.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Services/WorkspaceResourceAuthorizationService.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/DependencyInjection.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/MangakaChapterRepository.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.razor.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.Pages.cs`
- `docs/revision/Editor/2026-07-23-editor-workspace-loading-jwt-regression-fix.md`

## Deferred and transitional behavior

`SeriesController.GetWorkspaceEntryAsync` and `SeriesApiClient.GetWorkspaceEntryAsync` remain unchanged and continue to use the explicitly deferred `X-Actor-User-Id` transport. No Batch A/B/C controller or client header handling was restored.

## Verification status

- Static action-role and resource-guard inspection: performed.
- `git diff --check`: passed (line-ending warnings only).
- Build: **NOT RUN — user will build**.
- Tests: **NOT RUN — per user instruction**.
- Manual black-box testing: **NOT RUN — user will test**.
- Application/Infrastructure impact: one Application authorization interface and one minimal EF Infrastructure implementation; no workflow or persistence contract changes.
- Database/stored procedure impact: none.
- Commit/push: none.
