# Mangaka Series Proposal Filters and Targeted Card Refresh

## Branch

`feature/Mangaka`

## Date

2026-06-21

## Task summary

Two main improvements:
1. **Phase 1 — Targeted single-card reload (Option B improvement):** Replaced full `LoadSeriesAsync()` reload after `SaveDraftEditAsync` with a targeted `GetMySeriesCardByIdAsync` single-card query. The update command remains minimal; a separate CQRS query fetches the fresh read model for just the updated card.
2. **Phase 2 — Mangaka Series Proposal filters:** Updated the proposals tab search to title-only, added genre/tag filter using `ReferenceMultiSelectFilter`, added selected chip display and Clear filters button.

**Phase 3 (proposal UI unification) and Phase 4 (picker popup placement) were deferred** and not implemented in this session.

## Architecture path

### Targeted single-card reload flow

```
Blazor Web (MangakaDashboard.razor / SaveDraftEditAsync)
  → IMangakaSeriesApiClient.UpdateDraftAsync (command — minimal result)
  → API: PUT /api/mangaka/series/{seriesId}/draft-profile
  → MediatR: UpdateSeriesDraftCommand → Handler → SP wrapper
  → SQL Server: manga.usp_Series_UpdateProfile

Then:

Blazor Web (MangakaDashboard.razor / SaveDraftEditAsync)
  → IMangakaSeriesApiClient.GetMySeriesCardByIdAsync (query — targeted read)
  → API: GET /api/mangaka/series/{seriesId}/card
  → MediatR: GetMyMangakaSeriesCardByIdQuery → Handler → EF read
  → SQL Server: EF AsNoTracking query
```

### Proposal filter flow

```
Blazor Web (MangakaDashboard.razor / proposals tab)
  → IMangakaSeriesApiClient.GetMySeriesProposalsAsync (full list, once)
  → AllFilteredProposals (client-side filter via title, genres, tags, status, sort)
```

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Domain | `Interfaces/ISeriesRepository.cs` | Added `GetByContributorAndSeriesIdAsync` method |
| Infrastructure | `Repositories/SeriesRepository.cs` | Implemented `GetByContributorAndSeriesIdAsync` (EF AsNoTracking, same actor scoping as dashboard list query) |
| Application (new) | `Features/Mangaka/Series/Queries/GetMyMangakaSeriesCardById/GetMyMangakaSeriesCardByIdQuery.cs` | New query record |
| Application (new) | `Features/Mangaka/Series/Queries/GetMyMangakaSeriesCardById/GetMyMangakaSeriesCardByIdQueryHandler.cs` | New query handler (maps Series entity → SeriesDto, same pattern as list query) |
| API | `Controllers/MangakaSeriesController.cs` | Added `GET /api/mangaka/series/{seriesId}/card` endpoint + using for new query namespace |
| Web Client | `Services/Api/IMangakaSeriesApiClient.cs` | Added `GetMySeriesCardByIdAsync` method |
| Web Client | `Services/Api/MangakaSeriesApiClient.cs` | Implemented `GetMySeriesCardByIdAsync` (HTTP GET, returns null on 404) |
| Web UI | `Components/Pages/Mangaka/MangakaDashboard.razor` | Changed `SaveDraftEditAsync` to use targeted query; updated proposal tab with genre/tag filters |

## DB/SP impact

None. No stored procedures, tables, or migrations changed.

## Phase 1 — Targeted single-card reload implementation

### Problem
`SaveDraftEditAsync` called `LoadSeriesAsync()` to reload the entire dashboard list after each update.

### Solution
New targeted query flow:
1. `UpdateDraftAsync` (command) — minimal `SeriesDraftUpdatedDto` result (unchanged)
2. `GetMySeriesCardByIdAsync` (query) — fresh `SeriesDto` from the server with accurate timestamps and cover URL
3. Replace only the single card in `_seriesData`
4. Existing filters/sorting recompute automatically via computed properties

### New endpoint

```
GET /api/mangaka/series/{seriesId}/card
```

- Returns `SeriesDto` for the requested series if the current actor is an active Mangaka contributor
- Returns 404 if the series is not found or the actor is not an active contributor
- Uses same actor scoping as `GET /api/mangaka/series/my-series` (active contributor, ACTIVE account, Mangaka role)

### New query

```
GetMyMangakaSeriesCardByIdQuery(ActorUserId, SeriesId) → SeriesDto?
```

Handler uses `ISeriesRepository.GetByContributorAndSeriesIdAsync` — same EF AsNoTracking query with CoverFile/Genres/Tags includes, filtered to one SeriesId.

### Fallback behavior
If `GetMySeriesCardByIdAsync` returns null (rare: server can't find the series after update), the code falls back to constructing the card from the command result + local genre/tag selections. This handles edge cases like contributor access being revoked between update and read.

## Phase 2 — Mangaka Series Proposal filters

### Search
- **Before:** "Search by title or genre" — searched SeriesTitle, ProposalTitle, AND genre names
- **After:** "Search by title" — searches SeriesTitle and ProposalTitle only

### Genre filter
- `ReferenceMultiSelectFilter` with `TItem="GenreDto"`
- Filters proposals where ALL selected genres match (p.Genres contains all selected IDs)
- Selected genre chips with × remove

### Tag filter
- `ReferenceMultiSelectFilter` with `TItem="TagDto"`
- Filters proposals where ALL selected tags match (p.Tags contains all selected IDs)
- Selected tag chips with × remove

### Clear filters
- Shows when any genre or tag filter is active
- Clears both genre and tag filter selections
- Does not clear status chips or search text

### State variables added
- `_proposalFilterGenreIds: HashSet<Guid>`
- `_proposalFilterTagIds: HashSet<Guid>`

## Phase 3 — Proposal UI unification

**Deferred.** Mangaka and Editor proposal UIs have different data scoping (Mangaka sees only own series proposals; Editor sees all), different actions (Editor has claim/review/cancel; Mangaka is read-only), different navigation patterns, and different search fields. Risks of unifying incorrectly outweigh the benefits at this stage.

## Phase 4 — Create/edit picker popup placement

**Deferred.** The inline picker in the modal body is stable. Changing it now risks reintroducing overlay clipping issues that were already fixed. No `GenreTagSelectionField.razor` or `GenreTagPickerDialog.razor` were created.

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
Build succeeded.
0 errors
57 warnings (all pre-existing baseline, none from changed files)
```

## Manual smoke

Runtime smoke not run; user must verify manually.

### Checklist

```
[ ] Login as Mangaka
[ ] Open /mangaka
[ ] Update one draft
[ ] Only that one card is reloaded (not full list)
[ ] UpdatedAtUtc / "Updated just now" refreshes correctly
[ ] Title/genres/tags/cover remain correct after update
[ ] Open Series Proposals tab
[ ] Search box filters by title only (not genre names)
[ ] Searching a genre name via text search no longer matches
[ ] Genre filter works (select genres → matching proposals shown)
[ ] Tag filter works (select tags → matching proposals shown)
[ ] Clear filters clears genre/tag selections
[ ] Status chips still work
[ ] Sort still works
[ ] Proposal detail modal works (open/download files)
[ ] No editor-only actions appear for Mangaka
[ ] Editor proposal UI still works independently
```

## Known issues

None from this session.

## Follow-ups

- Runtime smoke testing recommended
- UI unification (Phase 3) — reconsider after proposal patterns stabilize
- Picker popup placement (Phase 4) — only if user reports issues with inline picker
