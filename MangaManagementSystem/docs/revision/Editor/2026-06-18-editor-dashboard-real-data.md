# Editor Dashboard Real Data

## Branch

`feature/Mangaka`

## Date

2026-06-18

## Task Summary

Replaced the hard-coded mock data on the Tantou Editor Dashboard (`/editor`) with real
database values served through the project's canonical architecture path. The dashboard
previously injected nothing and rendered static KPI numbers and fabricated manga/user rows.
It now loads a real read model through a new typed API client, with loading, error/retry,
and empty states. A pre-existing navigation bug (Review button navigating with `SeriesId`
instead of `SeriesProposalId`) was also fixed.

## Architecture Path

```
EditorDashboard.razor
  -> IEditorDashboardApiClient.GetDashboardAsync(actorUserId)   (X-Actor-User-Id header)
  -> GET /api/editor/dashboard
  -> EditorDashboardController.GetDashboardAsync
  -> IMediator.Send(GetEditorDashboardQuery)
  -> GetEditorDashboardQueryHandler
  -> IEditorDashboardRepository.GetDashboardDataAsync (EF Core AsNoTracking reads)
  -> SQL Server
```

No direct Web-to-Application, Web-to-Infrastructure, Web-to-EF, or Web-to-stored-procedure
calls were added. No proposal review write commands or stored procedures were modified.

## Mock Data Removed

From `EditorDashboard.razor`:

- KPI fields: `_pendingProposals = 3`, `_chaptersUnderReview = 5`, `_pendingTasks = 8`,
  `_rankedSeries = 12`.
- Hard-coded `Proposals` list: "Crimson Moon Academy", "Neon Samurai Path",
  "Lunar Eclipse Academy" with fake submitters ("Ken Sato", "Yuki Tanaka",
  "Hiroshi Yamamoto").
- Hard-coded `RecentSeries` list: "Crimson Moon Academy", "Neon Samurai Path",
  "Shadow Garden Tales" with fake chapter labels and timestamps.
- Local `ProposalRow` / `SeriesRow` record types.
- Navigation bug: `OpenProposal(Guid seriesId)` now receives and navigates with the
  `SeriesProposalId`.

(Note: other pages such as `ChapterReviewList.razor`, `AnnotationWorkspace.razor`,
`SeriesRanking.razor`, `BoardPolls.razor` still contain mock data but are out of scope for
this task.)

## Files Changed

### Added

| File | Purpose |
|------|---------|
| `src/MangaManagementSystem.Application/DTOs/Editor/EditorDashboardDtos.cs` | `EditorDashboardDto`, `EditorDashboardProposalDto`, `EditorDashboardSeriesActivityDto` |
| `src/MangaManagementSystem.Application/Features/Editor/Dashboard/Queries/GetEditorDashboard/GetEditorDashboardQuery.cs` | MediatR query record |
| `src/MangaManagementSystem.Application/Features/Editor/Dashboard/Queries/GetEditorDashboard/GetEditorDashboardQueryHandler.cs` | Maps Domain read data to DTOs (preview limit = 5 each) |
| `src/MangaManagementSystem.Domain/Interfaces/IEditorDashboardRepository.cs` | Read-only repository interface + `EditorDashboardData` Domain result record |
| `src/MangaManagementSystem.Infrastructure/Repositories/EditorDashboardRepository.cs` | EF Core `AsNoTracking` implementation (counts + preview lists) |
| `src/MangaManagementSystem.API/Controllers/Editor/EditorDashboardController.cs` | Thin controller, `X-Actor-User-Id` header, one `IMediator.Send` |
| `src/MangaManagementSystem.Web/Services/Api/IEditorDashboardApiClient.cs` | Typed client interface |
| `src/MangaManagementSystem.Web/Services/Api/EditorDashboardApiClient.cs` | HttpClient implementation with safe error parsing |

### Modified

| File | Change |
|------|--------|
| `src/MangaManagementSystem.Web/Components/Pages/Editor/EditorDashboard.razor` | Removed all mock data; loads `EditorDashboardDto` via typed client; loading/error+retry/empty states; fixed Review navigation to `SeriesProposalId`; recent series title links to `/series/{slug}` |
| `src/MangaManagementSystem.Web/Program.cs` | Registered `IEditorDashboardApiClient` / `EditorDashboardApiClient` via `AddHttpClient` |
| `src/MangaManagementSystem.Infrastructure/DependencyInjection.cs` | Registered `IEditorDashboardRepository` / `EditorDashboardRepository` |

## Endpoint Added

- `GET /api/editor/dashboard` — returns `EditorDashboardDto`. Requires the transitional
  `X-Actor-User-Id` header (same pattern as the proposal review workflow). Returns HTTP 400
  if the actor header is missing/invalid, HTTP 500 on unexpected error.

## Query / Repository Methods Added

- **Query**: `GetEditorDashboardQuery` → `GetEditorDashboardQueryHandler` (returns
  `EditorDashboardDto`; applies preview limits of 5 for both tables; derives the latest
  chapter label from each series' chapters).
- **Repository**: `IEditorDashboardRepository.GetDashboardDataAsync(int proposalQueueTake,
  int recentSeriesTake, CancellationToken)` implemented in `EditorDashboardRepository` using
  EF Core `AsNoTracking`. Returns the `EditorDashboardData` Domain record (4 counts + a
  proposal list + a recent-series list with eagerly-loaded chapters).

## Dashboard Data Mapping

| Dashboard element | Source | Rule |
|-------------------|--------|------|
| Pending Proposals | `SeriesProposal` | Count where `StatusCode == "UNDER_EDITORIAL_REVIEW"` (global; matches the shared editorial queue rule) |
| Chapters Under Review | `Chapter` | Count where `StatusCode == "UNDER_REVIEW"` (global MVP count) |
| Pending Annotations | `ChapterPageAnnotation` | Count where `ResolvedAtUtc == null` |
| Serialized Series | `Series` | Count where `StatusCode == "SERIALIZED"` |
| Proposal Review Queue | `SeriesProposal` | `UNDER_EDITORIAL_REVIEW`, ordered by `SubmittedAtUtc` desc, top 5; Review → `/editor/proposals/{SeriesProposalId}` |
| Recent Series Activity | `Series` (+ `Chapter`) | Ordered by `UpdatedAtUtc ?? CreatedAtUtc` desc, top 5; latest chapter label = most recently created chapter's `ChapterNumberLabel`; title links to `/series/{slug}` |

## UI Behavior

- Dashboard loads through `IEditorDashboardApiClient` only — no Application/Infrastructure/EF
  injection in the Razor page.
- **Loading state**: indeterminate progress bar while fetching.
- **Error state**: red alert with the failure message and a Retry button that re-calls the API.
- **Empty states**: "All Caught Up!" when no pending proposals; "No Recent Activity" when no
  series exist.
- KPI cards keep the original visual layout and link to real pages (`/editor/proposals`,
  `/editor/chapters`, `/editor/annotations`, `/ranking`).
- Review button navigates to `/editor/proposals/{seriesProposalId}` (bug fixed).
- Recent series title links to `/series/{slug}` when a slug is present.
- Status codes are rendered with underscores replaced by spaces and color-coded.

## Build Result

```
dotnet build MangaManagementSystem\MangaManagementSystem.sln
```

- **Build succeeded**
- **0 errors**
- **58 warnings**

The 58 warnings are all pre-existing categories (`MUD002`, `CS0649`, `CS0414`, `CS8981`).
A filtered build check for dashboard-related files produced no output, confirming **no new
warnings were introduced by this task**. (One transient build error — `Series` namespace vs
type collision in the handler — was fixed by fully qualifying
`MangaManagementSystem.Domain.Entities.Series`.)

## Manual Test Checklist

1. [ ] Login as active Tantou Editor.
2. [ ] Open Editor Dashboard (`/editor`).
3. [ ] KPI cards load real counts from the database.
4. [ ] Proposal Review Queue shows real proposals, not the Crimson Moon / Neon Samurai mock rows.
5. [ ] Review button opens `/editor/proposals/{seriesProposalId}`.
6. [ ] Recent Series Activity shows real series data or an empty state.
7. [ ] Refresh/retry works after an API error.
8. [ ] Empty states show correctly when no data exists.
9. [ ] No direct Web-to-Application service injection remains in the dashboard.
10. [ ] Build succeeds with 0 errors.

## Remaining Tasks

- Manual browser/database verification still recommended (real Tantou Editor login, real
  proposals/series/chapters/annotations).
- **Per-editor scoping**: Chapters Under Review and Pending Annotations are currently global
  MVP counts. If a Tantou Editor contributor scope is later defined for chapter review and
  annotations, these queries should be narrowed accordingly.
- Recent Series Activity ordering uses `UpdatedAtUtc ?? CreatedAtUtc`. If a richer activity
  signal (latest proposal submitted/reviewed, latest chapter released) is desired, the
  ordering rule can be refined.
- Other Editor pages (`ChapterReviewList.razor`, `AnnotationWorkspace.razor`) and Ranking /
  Board pages still contain mock data and remain future work.
