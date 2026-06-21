# Series Genre/Tag Reference Lookup + UI (Phase 4)

**Branch:** `feature/Mangaka`
**Date:** 2026-06-20
**Scope:** Reference data API + MangakaDashboard multi-select genre/tag UI
**Related workflow:** BF-SERIES-001, BF-SERIES-002, dashboard display

## Task summary

Phase 4 of genre/tag normalization. Created reference data lookup backend (genres/tags), typed API client, and updated MangakaDashboard create/edit draft dialogs to use real genre/tag multi-select controls. Create/edit now passes real `GenreIds` and `TagIds` to the API.

## Architecture path

### Reference data (read):

```
MangakaDashboard
  → ReferenceDataApiClient
  → GET /api/reference/genres, /api/reference/tags
  → ReferenceDataController
  → MediatR GetGenresQuery / GetTagsQuery
  → Application handler
  → IReferenceDataRepository.GetGenresAsync / GetTagsAsync
  → Infrastructure ReferenceDataRepository (EF AsNoTracking)
  → SQL Server
```

### Create/edit (write — unchanged from Phase 3):

```
MangakaDashboard
  → MangakaSeriesApiClient
  → MangakaSeriesController
  → MediatR command
  → ISeriesRepository
  → SQL Server stored procedure
```

## Files changed

### Domain interfaces (1 file)

| File | Change |
|------|--------|
| `Domain/Interfaces/IReferenceDataRepository.cs` | **Created** — `GetGenresAsync`, `GetTagsAsync` returning `IReadOnlyList<Genre>`/`<Tag>` |

### Infrastructure (2 files)

| File | Change |
|------|--------|
| `Infrastructure/Repositories/ReferenceDataRepository.cs` | **Created** — EF `AsNoTracking().OrderBy(g => g.GenreName).ToListAsync()` |
| `Infrastructure/DependencyInjection.cs` | Registered `IReferenceDataRepository` → `ReferenceDataRepository` (scoped) |

### Application queries (4 files)

| File | Change |
|------|--------|
| `Features/ReferenceData/Queries/GetGenres/GetGenresQuery.cs` | **Created** — empty query |
| `Features/ReferenceData/Queries/GetGenres/GetGenresQueryHandler.cs` | **Created** — maps `Genre` → `GenreDto` |
| `Features/ReferenceData/Queries/GetTags/GetTagsQuery.cs` | **Created** — empty query |
| `Features/ReferenceData/Queries/GetTags/GetTagsQueryHandler.cs` | **Created** — maps `Tag` → `TagDto` |

### API controller (1 file)

| File | Change |
|------|--------|
| `API/Controllers/ReferenceDataController.cs` | **Created** — `GET /api/reference/genres`, `GET /api/reference/tags` |

### Web typed API client (3 files)

| File | Change |
|------|--------|
| `Web/Services/Api/IReferenceDataApiClient.cs` | **Created** — `GetGenresAsync()`, `GetTagsAsync()` |
| `Web/Services/Api/ReferenceDataApiClient.cs` | **Created** — `HttpClient.GetAsync` + `ReadFromJsonAsync<List<GenreDto/TagDto>>` |
| `Web/Program.cs` | Registered `IReferenceDataApiClient` → `ReferenceDataApiClient` typed `HttpClient` |

### Web UI (1 file)

| File | Change |
|------|--------|
| `Web/Components/Pages/Mangaka/MangakaDashboard.razor` | Replaced hardcoded `_genres`/`_newDraftGenre`/`_editGenre` with `_availableGenres`/`_availableTags` from API, `HashSet<Guid>` for selections, checkbox lists for multi-select, real `GenreIds`/`TagIds` in API calls |

## Reference data API

| Endpoint | Method | Response |
|----------|--------|----------|
| `GET /api/reference/genres` | GET | `IReadOnlyList<GenreDto>` (sorted by GenreName) |
| `GET /api/reference/tags` | GET | `IReadOnlyList<TagDto>` (sorted by TagName) |

Architecture: Controller → MediatR query → handler → repository → EF `AsNoTracking()` → `manga.Genre` / `manga.Tag`.

## MangakaDashboard UI behavior

### Genres/Tags loading
- `OnInitializedAsync` calls `LoadReferenceDataAsync()` which fetches genres and tags via `IReferenceDataApiClient`
- Stores results in `_availableGenres` and `_availableTags`
- On failure, shows empty lists (no crash)

### Create Draft dialog
- Genre multi-select: `MudCheckBox` list from `_availableGenres`, toggles `_newDraftGenreIds` (HashSet)
- Tags multi-select: `MudCheckBox` list from `_availableTags`, toggles `_newDraftTagIds` (HashSet)
- At least one genre required (Create button disabled when `_newDraftGenreIds.Count == 0`)
- Tags optional
- On submit: passes `_newDraftGenreIds.ToList()` and `_newDraftTagIds.ToList()` to `MangakaSeriesApiClient.CreateDraftAsync`

### Edit Draft dialog
- Pre-populated from `series.Genres.Select(g => g.GenreId)` and `series.Tags.Select(t => t.TagId)` stored in `SeriesCardData`
- Same checkbox list controls as create
- On save: passes `_editGenreIds.ToList()` and `_editTagIds.ToList()` to `MangakaSeriesApiClient.UpdateDraftAsync`
- Updated card reflects new selections

### Dashboard display
- `SeriesCardData` now carries `IReadOnlyList<GenreDto> Genres` and `IReadOnlyList<TagDto> Tags` for edit pre-population
- Display chips and search unchanged from Phase 2

## Build result

```
dotnet build --no-incremental
0 errors, 65 warnings
```

Within baseline range (53–65). No new warnings from changed files (all warnings pre-existing MudBlazor/CS8602).

## Remaining legacy usage

### Safe normalized names
- `GenreDto.GenreId`/`GenreDto.GenreName` — current contracts, fine
- `TagDto.TagId`/`TagDto.TagName` — current contracts, fine

### Proposal submit SQL issue
- `SeriesProposal.cs:13` — `GenreSnapshot` unmapped entity property
- `SeriesProposalConfiguration.cs:14` — `builder.Ignore(sp => sp.GenreSnapshot)`
- `SeriesProposalDtos.cs:63` — `CreateProposalDto.GenreSnapshot` (write DTO, transitional)
- `SeriesProposalService.cs:78` — `GenreSnapshot = dto.GenreSnapshot` (legacy service)
- `manga.usp_SeriesProposal_Submit` — known genre_snapshot INSERT mismatch bug

### Transitional legacy service usage
- `CreateSeriesDraftDto.cs:15` — `string Genre` (legacy DTO, unused by CQRS path)
- `SeriesDtos.cs:34,47` — `CreateSeriesDto.Genre`, `UpdateSeriesDto.Genre` (legacy DTOs)
- `SeriesService.cs:76` — `dto.Genre` validation (legacy service path)

### Should be removed later
- No urgent items; all remaining legacy is transitional and doesn't block the CQRS path.

## Confirmed constraints

- No `SeriesGenre` C# entity created.
- No `SeriesTag` C# entity created.
- No SQL tables created.
- No migration created.
- No stored procedure changed.
- No workspace/AI/task/annotation/chapter/Auth/Admin/Board changes.
- No `/mangaka/workspace/{SeriesId}` restoration.
- No `ISeriesService.GetAllSeriesAsync` reintroduction.

## Manual verification checklist

1. [x] Build succeeds with 0 errors
2. [ ] Open `/mangaka` — genres/tags load from API
3. [ ] Create Draft: genre checkboxes visible, at least one genre required
4. [ ] Create Draft: tags optional, can select multiple
5. [ ] Create Draft: sends real `GenreIds` and `TagIds`
6. [ ] Edit Draft: opens with existing genres/tags pre-selected
7. [ ] Save Edit: sends updated `GenreIds` and `TagIds`
8. [ ] Dashboard cards: genre/tag chips display correctly
9. [ ] API: `GET /api/reference/genres` returns genre list
10. [ ] API: `GET /api/reference/tags` returns tag list
11. [ ] API: create/update rejects empty genre IDs with 400
