# Session Note — Mangaka Dashboard Scope and Filters

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-18

---

## Task Summary

Fix the Mangaka dashboard data scope so it returns only series where the logged-in actor
is an active Mangaka contributor, and add client-side status filtering, search, and sort
to improve dashboard usability.

Previously the dashboard loaded every series in the database via
`ISeriesService.GetAllSeriesAsync()`, which called `ISeriesRepository.GetAllWithCoverAsync()`
with no actor filter. Any logged-in Mangaka saw all series, not just their own.

---

## Problem

`MangakaDashboard.razor` called `SeriesService.GetAllSeriesAsync()` directly (legacy
pre-CQRS pattern). This returned all series from the database. The dashboard displayed
series cards for every Mangaka user, not just the current user's own contributed series.

Additionally, the dashboard had no status filtering, search, or sort capabilities,
making it hard to find specific series as the catalog grows.

---

## Architecture Flow

```
MangakaDashboard.razor
  → IMangakaSeriesApiClient.GetMySeriesAsync(actorUserId)
  → GET /api/mangaka/series/my-series
  → MangakaSeriesController.GetMySeriesAsync  (thin API controller)
  → IMediator.Send(GetMyMangakaSeriesQuery)  (MediatR dispatch)
  → GetMyMangakaSeriesQueryHandler  (Application — maps entities to DTOs)
  → ISeriesRepository.GetByActiveContributorWithCoverAsync(actorUserId)  (Infrastructure)
  → EF Core read query  (no stored procedure — read model only)
```

Auth boundary: transitional `X-Actor-User-Id` header pattern, same as all other
Mangaka workflows.

---

## Server-side Scope Rule

The EF read query filters by all of these conditions simultaneously:

```
SeriesContributor.UserId == actorUserId
AND SeriesContributor.EndDate IS NULL   (active contributor)
AND User.StatusCode == "ACTIVE"         (active account)
AND User.Role.RoleName == "Mangaka"     (Mangaka role)
```

If the actor has no qualifying contributor rows, the query returns an empty list.
No error is thrown — the dashboard simply shows zero cards with an appropriate
empty state message.

---

## Files Changed

### New Files

| File | Layer | Description |
|------|-------|-------------|
| `Features/Mangaka/Series/Queries/GetMyMangakaSeries/GetMyMangakaSeriesQuery.cs` | Application | `IRequest<IReadOnlyList<SeriesDto>>` record with `Guid ActorUserId` |
| `Features/Mangaka/Series/Queries/GetMyMangakaSeries/GetMyMangakaSeriesQueryHandler.cs` | Application | Handler: validates input, calls repository, maps `Series` → `SeriesDto` including `CoverUrl` |

### Modified Files

| File | Layer | Change |
|------|-------|--------|
| `Domain/Interfaces/ISeriesRepository.cs` | Domain | Added `GetByActiveContributorWithCoverAsync(Guid, CancellationToken)` |
| `Infrastructure/Repositories/SeriesRepository.cs` | Infrastructure | Implemented `GetByActiveContributorWithCoverAsync` — EF read query with `Include(CoverFile)`, `AsNoTracking`, filtered by contributor membership |
| `API/Controllers/MangakaSeriesController.cs` | API | Added `GET my-series` endpoint; added `using` for query type and `IReadOnlyList` |
| `Web/Services/Api/IMangakaSeriesApiClient.cs` | Web | Added `GetMySeriesAsync(Guid, CancellationToken)` interface method |
| `Web/Services/Api/MangakaSeriesApiClient.cs` | Web | Implemented `GetMySeriesAsync` — GET request with actor header, deserializes `List<SeriesDto>` |
| `Web/Components/Pages/Mangaka/MangakaDashboard.razor` | Web | (1) Removed `@inject ISeriesService SeriesService`; (2) `LoadSeriesAsync` now calls `MangakaSeriesApiClient.GetMySeriesAsync`; (3) Added status filter chips with counts, search field, sort dropdown, and empty states |

---

## API Endpoint Added

```
GET /api/mangaka/series/my-series
Header: X-Actor-User-Id: <actorUserId>

200 OK → List<SeriesDto>  (only series where actor is active Mangaka contributor)
400 BadRequest → ApiErrorResponse { message }  (missing actor)
500 Problem → generic safe message
```

---

## Application Query Added

```csharp
GetMyMangakaSeriesQuery(Guid ActorUserId)
  → IRequest<IReadOnlyList<SeriesDto>>
```

Handler responsibilities:
- Validates `ActorUserId != Guid.Empty`.
- Calls `ISeriesRepository.GetByActiveContributorWithCoverAsync`.
- Maps each `Series` entity to `SeriesDto`, including `CoverUrl` from
  `CoverFile?.CloudinarySecureUrl` (only when not soft-deleted).
- Returns empty list if actor is empty or has no qualifying series.

---

## Repository Read Query

`ISeriesRepository.GetByActiveContributorWithCoverAsync(Guid actorUserId, CancellationToken)`

Implementation uses a single EF query with `Include` and subquery:

```csharp
return await _context.Series
    .AsNoTracking()
    .Include(s => s.CoverFile)
    .Where(s => _context.SeriesContributors.Any(sc =>
        sc.SeriesId == s.SeriesId &&
        sc.UserId == actorUserId &&
        sc.EndDate == null &&
        sc.User != null &&
        sc.User.StatusCode == "ACTIVE" &&
        sc.User.Role != null &&
        sc.User.Role.RoleName == "Mangaka"))
    .ToListAsync(cancellationToken);
```

No stored procedure — this is a read-only query. Per architecture rules, EF read
queries are acceptable for read models when no state transition, audit, or
concurrency concern is involved.

---

## Dashboard UI Changes

### Removed
- `@inject ISeriesService SeriesService` — no longer used in this file.
- `SeriesService.GetAllSeriesAsync()` call in `LoadSeriesAsync()`.

### Added — Filter/Search/Sort
- **Status filter chips:** Rendered above the card grid as styled `<button>` elements.
  Groups: All, Draft, In Review, Serialized, Paused/Hiatus, Cancelled, Completed.
  Each shows a count based on the scoped `_seriesData` (server-filtered).
- **Search field:** `MudTextField` with search icon, filters by title or genre
  (case-insensitive), immediate filtering on input.
- **Sort dropdown:** `MudSelect` with three options — Recently Updated (default),
  Newest First, Title A-Z.
- **Selected filter styling:** Active chip uses indigo background (`#4f46e5`) with
  white text; inactive chips use light gray background with gray text.

### Added — Empty States
- Each filter group has a contextual empty state message:
  - All: "You don't have any series yet. Create a new draft to get started."
  - Draft: "No draft series. Create a new draft to get started."
  - In Review: "No series currently under review."
  - Serialized: "No serialized series yet."
  - Paused/Hiatus: "No series on hiatus."
  - Cancelled: "No cancelled series."
  - Completed: "No completed series."
  - Search (no match): "No series match your search."
- Empty states include a "New Series Draft" button when appropriate.

### Header Subtitle
- Changed from "active projects in your catalog" to "series in your Mangaka dashboard"
  to reflect the scoped data.

### Unchanged
- All card click behaviors: Edit Draft modal, Review Status modal, `/series/{slug}`
  routing, Cancel Draft confirmation, Submit Proposal.
- All card actions: Create Draft, Edit Draft, Submit Proposal, Cancel Draft.
- All existing CQRS workflows: Create Draft, Edit Draft, Submit Proposal, Cancel Draft.

---

## Filter/Search/Sort Behavior

Filtering, searching, and sorting are all **client-side** over the server-scoped
`_seriesData`. The server already returns only the current Mangaka's series. The
client then applies:

1. **Status filter** — matches `StatusCode` against the selected group's status codes.
   "All" passes all series through.
2. **Search** — filters by title or genre containing the search term
   (case-insensitive).
3. **Sort** — orders by `UpdatedAtUtc ?? CreatedAtUtc` descending (recent),
   `CreatedAtUtc` descending (newest), or `Title` ascending (A-Z).

Filter counts are computed from `_seriesData` (already scoped), so counts always
reflect only the current Mangaka's series — never the global count.

---

## Build Result

`dotnet build MangaManagementSystem.slnx` — **Build succeeded, 0 errors.**

All warnings are pre-existing. No new warnings introduced by this task.

---

## Manual Test Checklist

```
1.  Login as Mangaka A. Create 2 series drafts. Confirm both appear on the dashboard.
2.  Login as Mangaka B. Create 1 series draft. Confirm it appears.
3.  Confirm Mangaka A does NOT see Mangaka B's series on their dashboard.
4.  Confirm Mangaka B does NOT see Mangaka A's series on their dashboard.
5.  On Mangaka A's dashboard: confirm "All (2)" count is correct.
6.  Click "Draft (2)" chip — confirm only draft series shown, count correct.
7.  Submit proposal on one series. Confirm "Draft (1)" and "In Review (1)" counts update.
8.  Test "Serialized", "Paused / Hiatus", "Cancelled", "Completed" filters (with seed data).
9.  Type a title in the search box — confirm only matching series shown.
10. Type a genre in the search box — confirm only matching series shown.
11. Type gibberish — confirm empty state: "No series match your search."
12. Switch sort to "Newest First" — confirm order by creation date.
13. Switch sort to "Title A-Z" — confirm alphabetical order.
14. Switch sort to "Recently Updated" — confirm most recently updated first.
15. Confirm Create Draft workflow still works end-to-end.
16. Confirm Edit Draft workflow still works end-to-end.
17. Confirm Submit Proposal workflow still works end-to-end.
18. Confirm Cancel Draft workflow still works end-to-end.
19. Confirm card click routing is unchanged: PROPOSAL_DRAFT → Edit modal, REVIEW → Review Status modal, SERIALIZED → /series/{slug}.
20. Confirm no raw SQL or stack trace appears in UI or API responses.
```

---

## Remaining Tasks

- **`/series/{slug}` full page** — stub only; chapter list and workspace entry pending.
- **Slug preview in Edit Draft modal** — derived slug not shown to user before saving.
- **Full API authentication** — transitional `X-Actor-User-Id` header still in use.
- **`ISeriesService` cleanup** — `GetAllSeriesAsync()` is no longer called from the API
  controller but remains registered for other consumers (CreatorWorkspace, BoardPolls,
  SeriesPage, etc.). Can be deprecated once all callers are migrated to typed API clients.
