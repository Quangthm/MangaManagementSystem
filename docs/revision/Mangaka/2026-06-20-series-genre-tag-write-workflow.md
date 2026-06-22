# Series Genre/Tag Write Workflow (Phase 3)

**Branch:** `feature/Mangaka`
**Date:** 2026-06-20
**Scope:** Create/edit draft write path from string genre → normalized genre/tag IDs
**Related workflow:** BF-SERIES-001, BF-SERIES-002

## Task summary

Phase 3 of genre/tag normalization. Updated the create/edit series draft write workflows from the old `string genre` scalar to `IReadOnlyList<Guid> GenreIds` and `IReadOnlyList<Guid> TagIds`. The C# write path now calls the updated stored procedures with `@genre_ids_json` and `@tag_ids_json` JSON arrays instead of the old `@genre` scalar parameter.

## Architecture path

```
MangakaDashboard (create/edit dialog)
  → typed MangakaSeriesApiClient (repeated form fields: GenreIds, TagIds)
  → ASP.NET Core MangakaSeriesController
  → MediatR CreateSeriesDraftCommand / UpdateSeriesDraftCommand
  → Application handler (validates GenreIds non-empty, cleans dupes, Guid.Empty)
  → ISeriesRepository.CreateSeriesDraftViaProcAsync / UpdateSeriesDraftViaProcAsync
  → Infrastructure SeriesRepository (JsonSerializer.Serialize → @genre_ids_json / @tag_ids_json)
  → SQL Server manga.usp_Series_Create / manga.usp_Series_UpdateProfile
```

## Files changed

### Domain interfaces (1 file)

| File | Change |
|------|--------|
| `Domain/Interfaces/ISeriesRepository.cs` | `string genre` → `IReadOnlyList<Guid> genreIds, IReadOnlyList<Guid> tagIds` in both `CreateSeriesDraftViaProcAsync` and `UpdateSeriesDraftViaProcAsync` |

### Infrastructure repository (1 file)

| File | Change |
|------|--------|
| `Infrastructure/Repositories/SeriesRepository.cs` | Added `System.Text.Json`, `System.Data.Common`, `System.Linq` usings. Both SP wrapper methods: parameter binding `@genre` → `@genre_ids_json` + `@tag_ids_json`. Added `SerializeGuidArray` helper (filters Guid.Empty, dedup, serializes to JSON) |

### Application commands (2 files)

| File | Change |
|------|--------|
| `Features/Mangaka/Series/Commands/CreateSeriesDraft/CreateSeriesDraftCommand.cs` | `string Genre` → `IReadOnlyList<Guid> GenreIds, IReadOnlyList<Guid> TagIds` |
| `Features/Mangaka/Series/Commands/UpdateSeriesDraft/UpdateSeriesDraftCommand.cs` | Same |

### Application command handlers (2 files)

| File | Change |
|------|--------|
| `Features/Mangaka/Series/Commands/CreateSeriesDraft/CreateSeriesDraftCommandHandler.cs` | Genre validation: checks `CleanGuidList(GenreIds).Count > 0` instead of `!string.IsNullOrWhiteSpace(genre)`. Added `CleanGuidList` helper (filters empty, dedup). Passes `genreIds`/`tagIds` to repository |
| `Features/Mangaka/Series/Commands/UpdateSeriesDraft/UpdateSeriesDraftCommandHandler.cs` | Same pattern |

### API contracts (2 files)

| File | Change |
|------|--------|
| `API/Contracts/CreateSeriesDraftForm.cs` | `string Genre` → `List<Guid> GenreIds, List<Guid> TagIds` |
| `API/Contracts/UpdateSeriesDraftForm.cs` | Same |

### API controller (1 file)

| File | Change |
|------|--------|
| `API/Controllers/MangakaSeriesController.cs` | CreateDraftAsync + UpdateDraftProfileAsync: genre validation `string.IsNullOrWhiteSpace(request.Genre)` → `request.GenreIds.Count == 0 \|\| request.GenreIds.All(id => id == Guid.Empty)`. Command mapping `Genre: request.Genre` → `GenreIds: request.GenreIds ?? new(), TagIds: request.TagIds ?? new()` |

### Web typed API client (2 files)

| File | Change |
|------|--------|
| `Web/Services/Api/IMangakaSeriesApiClient.cs` | `string genre` → `IReadOnlyList<Guid> genreIds, IReadOnlyList<Guid> tagIds` in both `CreateDraftAsync` and `UpdateDraftAsync` |
| `Web/Services/Api/MangakaSeriesApiClient.cs` | Multipart form: single `"Genre"` field → repeated `"GenreIds"` + `"TagIds"` fields (foreach loop, skip Guid.Empty) |

### Web compile-only (1 file)

| File | Change |
|------|--------|
| `Web/Components/Pages/Mangaka/MangakaDashboard.razor` | API calls: `genre: _newDraftGenre` → `genreIds: new List<Guid>(), tagIds: new List<Guid>()` (temporary Phase 4 TODO bridge). Create/edit dialog visually unchanged. |

### Application legacy service (1 file)

| File | Change |
|------|--------|
| `Application/Services/SeriesService.cs` | `CreateSeriesDraftViaProcAsync` call: `genre` string → `new List<Guid>(), new List<Guid>()` (transitional — legacy path not used by CQRS flow) |

## Write workflow mapping

### Create Draft

```
CreateSeriesDraftForm.GenreIds (List<Guid>)     // from multipart form
CreateSeriesDraftForm.TagIds  (List<Guid>)

→ MangakaSeriesController.CreateDraftAsync
    validates GenreIds.Count > 0 && not all Guid.Empty
    maps to CreateSeriesDraftCommand(GenreIds:, TagIds:)

→ CreateSeriesDraftCommandHandler.Handle
    genreIds = CleanGuidList(command.GenreIds)  // removes Guid.Empty, dedupes
    tagIds   = CleanGuidList(command.TagIds)
    validates genreIds.Count > 0
    passes to _seriesRepository.CreateSeriesDraftViaProcAsync(genreIds, tagIds)

→ SeriesRepository.CreateSeriesDraftViaProcAsync
    @genre_ids_json = SerializeGuidArray(genreIds)  // e.g. '["guid-1","guid-2"]'
    @tag_ids_json   = SerializeGuidArray(tagIds)    // e.g. '["guid-1"]' or '[]'
    calls manga.usp_Series_Create (ADO.NET StoredProcedure)

→ SQL Server usp_Series_Create
    receives @genre_ids_json NVARCHAR(MAX)
    receives @tag_ids_json NVARCHAR(MAX)
    parses with OPENJSON, validates against manga.Genre/manga.Tag
    inserts into manga.SeriesGenre / manga.SeriesTag
```

### Update Draft Profile

Same flow through `UpdateSeriesDraftForm` → `UpdateSeriesDraftCommand` → `UpdateSeriesDraftCommandHandler` → `UpdateSeriesDraftViaProcAsync` → `manga.usp_Series_UpdateProfile`.

## Stored procedure parameter summary

- `manga.usp_Series_Create` receives `@genre_ids_json` and `@tag_ids_json` from C# write calls
- `manga.usp_Series_UpdateProfile` receives `@genre_ids_json` and `@tag_ids_json` from C# write calls
- Old `@genre` scalar parameter is **no longer used** by any C# create/update draft write path

## Frontend status

**MangakaDashboard create/edit UI still uses Phase 4 for runtime operation.**

The create/edit dialogs retain the hardcoded single-genre `MudSelect` dropdown with `_genres` array (8 values). The API client calls pass empty `List<Guid>()` for both `genreIds` and `tagIds`. At runtime, the controller will return 400 "At least one valid genre is required."

Phase 4 must:
1. Add reference data endpoints (`GET /api/reference/genres`, `GET /api/reference/tags`)
2. Add typed reference API client
3. Update MangakaDashboard to fetch genres/tags on load
4. Replace single-genre dropdown with multi-select controls that pass real GenreIds/TagIds

## Remaining legacy usage

### Phase 4 frontend UI

| File | Remaining usage |
|------|-----------------|
| `MangakaDashboard.razor:733,878,1067,1081,1418,1495,1543,1886,1918,1937` | `_newDraftGenre`, `_editGenre` strings, hardcoded `_genres` array, single-genre `MudSelect` dropdowns |

### Proposal submit SQL issue

| File | Remaining usage |
|------|-----------------|
| `SeriesProposalDtos.cs:63` | `CreateProposalDto.GenreSnapshot:string` (write DTO, transitional) |
| `SeriesProposal.cs:13` | `SeriesProposal.GenreSnapshot` (unmapped entity property, transitional) |
| `SeriesProposalConfiguration.cs:14` | `builder.Ignore(sp => sp.GenreSnapshot)` (safe, intentional) |
| `usp_SeriesProposal_Submit` (SQL) | INSERT has `genre_snapshot` column but no variable — known mismatch bug (not fixed) |

### Transitional legacy services (not CQRS path)

| File | Remaining usage |
|------|-----------------|
| `CreateSeriesDraftDto.cs:15` | `string Genre` — used by legacy `SeriesService.CreateSeriesDraftAsync` |
| `SeriesDtos.cs:34,47` | `CreateSeriesDto.Genre:string`, `UpdateSeriesDto.Genre:string` — used by legacy `SeriesService.CreateSeriesAsync`/`UpdateSeriesAsync` |
| `SeriesService.cs:76` | `dto.Genre` validation — transitional |
| `SeriesProposalService.cs:78` | `GenreSnapshot = dto.GenreSnapshot` — transitional, entity property unmapped |

### Safe leftovers (intentional)

| File | Remaining usage |
|------|-----------------|
| `GenreDto.cs` | `GenreDto.GenreName:string` — this is the new normalized DTO, fine |
| `Domain/Entities/Genre.cs` | `Genre.GenreName:string` — entity property, fine |

## Build result

```
dotnet build --no-incremental
0 errors, 57 warnings
```

Within baseline range (53–60). No new warnings from changed files.

## Confirmed constraints

- No `SeriesGenre` C# entity created.
- No `SeriesTag` C# entity created.
- No SQL tables created.
- No migration created.
- No stored procedure changed.
- No workspace/AI/task/annotation/chapter/Auth/Admin/Board changes.
- Old `@genre` scalar no longer used by C# create/update draft write calls.

## Manual verification checklist

1. [ ] Build succeeds with 0 errors
2. [ ] `manga.usp_Series_Create` receives valid `@genre_ids_json` / `@tag_ids_json` JSON arrays when called from C#
3. [ ] `manga.usp_Series_UpdateProfile` receives valid JSON arrays
4. [ ] Controller rejects empty genre ID list with 400
5. [ ] Controller rejects all-Guid.Empty genre ID list with 400
6. [ ] Handler deduplicates and filters Guid.Empty from genre/tag ID lists
7. [ ] API client sends repeated `GenreIds` / `TagIds` form fields correctly
8. [ ] MangakaDashboard create/edit dialogs still show existing UI (Phase 4 will replace)
9. [ ] Existing serialized series with genre/tag data display correctly on read screens

## Next recommended prompt

Phase 4: Add genre/tag reference lookup API (`GET /api/reference/genres`, `GET /api/reference/tags`) + update MangakaDashboard create/edit draft UI to fetch genres/tags and use multi-select controls that pass real `GenreIds`/`TagIds`.
