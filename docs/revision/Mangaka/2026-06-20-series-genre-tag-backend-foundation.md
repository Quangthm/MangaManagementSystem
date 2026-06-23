# Series Genre/Tag Backend Foundation (Phase 1)

**Branch:** `feature/Mangaka`
**Date:** 2026-06-20
**Scope:** Domain + EF mapping for normalized Series genres/tags
**Related workflow:** BF-SERIES-001, BF-SERIES-002, series detail reads

## Task summary

Phase 1 of genre/tag normalization. The database already had normalized `manga.Genre`, `manga.Tag`, `manga.SeriesGenre`, `manga.SeriesTag` tables with composite-PK junction tables and updated stored procedures (`usp_Series_Create`, `usp_Series_UpdateProfile`) accepting `@genre_ids_json`/`@tag_ids_json`. However, the C# codebase still used a flat `string Genre` scalar on `Series` and `string GenreSnapshot` on `SeriesProposal` — completely out of sync with the database.

This phase created the C# domain entities, EF Core many-to-many skip navigation mappings, DTO stubs, and fixed all compile errors without touching frontend UI.

## Architecture flow

```
Phase 1 (this session):
  Domain: Genre.cs, Tag.cs (new), Series.cs (modified)
  Infrastructure: GenreConfiguration.cs, TagConfiguration.cs (new),
    SeriesConfiguration.cs, SeriesProposalConfiguration.cs (modified),
    ApplicationDbContext.cs (modified)

Phase 2 (next):
  Application handlers → read DTOs with IReadOnlyList<GenreDto>/<TagDto>
  Read queries → Include/ThenInclude Genres/Tags

Phase 3 (later):
  Write commands → pass GenreIds/TagIds as JSON arrays to SPs
```

## Important decisions

- **No `SeriesGenre` / `SeriesTag` C# entity classes created** — used EF Core many-to-many skip navigation via `UsingEntity` pointing to existing physical junction tables `manga.SeriesGenre` and `manga.SeriesTag`.
- **`Series.Genre` scalar removed** — database no longer has `manga.Series.genre` column.
- **`SeriesProposal.GenreSnapshot` kept as unmapped entity property** — EF ignores it via `builder.Ignore()`. Removing it would cascade into 12+ handlers/DTOs/Razor files. Phase 2 will replace all `GenreSnapshot` usages with joins through `Series.Genres`.
- **No `genre_snapshot_json` / `tag_snapshot_json` columns added** to `SeriesProposal` — per project decision, proposals resolve genre/tag from related Series at read time.
- **Legacy `SeriesService` kept transitional** — removed `Genre` entity property assignments. Write paths through this service are already broken at runtime (they pass wrong SP params). Phase 3 will migrate to CQRS commands.
- **Read handlers compute `string Genre` from `Genres` navigation** — temporary: `string.Join(", ", s.Genres.Select(g => g.GenreName))`. Phase 2 will change DTOs to `IReadOnlyList<GenreDto>`.

## Files changed

### Domain (3 files)

| File | Action | Details |
|------|--------|---------|
| `Domain/Entities/Genre.cs` | **Created** | New entity: `GenreId`, `GenreName`, `Description`, `ICollection<Series> Series` |
| `Domain/Entities/Tag.cs` | **Created** | New entity: `TagId`, `TagName`, `Description`, `ICollection<Series> Series` |
| `Domain/Entities/Series.cs` | **Modified** | Removed `string Genre`. Added `ICollection<Genre> Genres` and `ICollection<Tag> Tags` navigation properties |

### Infrastructure (5 files)

| File | Action | Details |
|------|--------|---------|
| `Infrastructure/Persistence/Configurations/GenreConfiguration.cs` | **Created** | Maps to `manga.Genre`. Key on `genre_id`, unique index on `genre_name` |
| `Infrastructure/Persistence/Configurations/TagConfiguration.cs` | **Created** | Maps to `manga.Tag`. Key on `tag_id`, unique index on `tag_name` |
| `Infrastructure/Persistence/Configurations/SeriesConfiguration.cs` | **Modified** | Removed `Genre` property mapping (`s.Genre` column no longer exists) |
| `Infrastructure/Persistence/Configurations/SeriesProposalConfiguration.cs` | **Modified** | Removed `GenreSnapshot` mapping, added `builder.Ignore(sp => sp.GenreSnapshot)` |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | **Modified** | Added `DbSet<Genre>`, `DbSet<Tag>`, many-to-many skip-nav mappings for `Series→Genres` (through `manga.SeriesGenre`) and `Series→Tags` (through `manga.SeriesTag`). Updated `IApplicationDbContext` interface. |

### Application (5 files)

| File | Action | Details |
|------|--------|---------|
| `Application/DTOs/Manga/GenreDto.cs` | **Created** | `sealed record GenreDto(Guid GenreId, string GenreName)` |
| `Application/DTOs/Manga/TagDto.cs` | **Created** | `sealed record TagDto(Guid TagId, string TagName)` |
| `Application/Features/Mangaka/Series/Queries/GetMyMangakaSeries/GetMyMangakaSeriesQueryHandler.cs` | **Modified** | `s.Genre` → `string.Join(", ", s.Genres.Select(g => g.GenreName))` (temporary until Phase 2 DTO change) |
| `Application/Features/Series/Queries/GetSeriesBySlug/GetSeriesBySlugQueryHandler.cs` | **Modified** | Same compute-from-navigation pattern |
| `Application/Services/SeriesService.cs` | **Modified** | Removed `Genre = dto.Genre` entity assignments (lines 42, 189). MapToDto: `s.Genre` → `string.Join(", ", s.Genres...)` |

### Web (0 files)

No Web changes. All existing Razor references to `series.Genre` / `GenreSnapshot` still compile (those are DTO properties, unchanged).

## EF mapping summary

```
Genre  → manga.Genre    (genre_id PK, genre_name unique, description)
Tag    → manga.Tag      (tag_id PK, tag_name unique, description)
Series → manga.Series   (no genre column anymore)
Series.Genres  ←→ Genre   through manga.SeriesGenre (series_id, genre_id)
Series.Tags    ←→ Tag     through manga.SeriesTag   (series_id, tag_id)
SeriesProposal.GenreSnapshot → IGNORED by EF (column removed from DB)
```

**Confirmed:**
- No `SeriesGenre` C# entity was created.
- No `SeriesTag` C# entity was created.
- No SQL tables were created.
- No migration was created.
- No stored procedure was changed.

## Build result

```
dotnet build --no-incremental
0 errors, 53 warnings
```

Warning count decreased from baseline 60 → 53 (no new warnings from changed files; variation due to pre-existing warnings in files not recompiled in this run).

## Old genre/snapshot usage remaining

### Phase 2 cleanup — DTO properties (compile but use legacy flat string)

| File | Property | Remaining usage |
|------|----------|-----------------|
| `DTOs/Manga/SeriesDtos.cs` | `SeriesDto.Genre` | `string Genre` — Phase 2: replace with `IReadOnlyList<GenreDto> Genres` |
| `DTOs/Manga/SeriesDetailDtos.cs` | `SeriesDetailDto.Genre` | Same |
| `DTOs/Manga/SeriesDraftUpdatedDto.cs` | `SeriesDraftUpdatedDto.Genre` | Same |
| `DTOs/Manga/SeriesProposalDtos.cs` | 5 DTOs with `GenreSnapshot` | Phase 2: replace with `IReadOnlyList<string> GenreNames` |
| `DTOs/Manga/CreateSeriesDraftDto.cs` | `Genre` | Phase 3: replace with `IReadOnlyList<Guid> GenreIds` |

### Phase 3 cleanup — write command params (compile but wrong at runtime)

| File | Remaining usage |
|------|-----------------|
| `CreateSeriesDraftCommand.cs:21` | `string Genre` → needs `IReadOnlyList<Guid> GenreIds` |
| `UpdateSeriesDraftCommand.cs:21` | Same |
| `CreateSeriesDraftCommandHandler.cs:186` | Passes `string genre` → needs `@genre_ids_json` |
| `UpdateSeriesDraftCommandHandler.cs:143` | Same |
| `SeriesRepository.cs:104,208` | Passes `@genre` scalar → needs `@genre_ids_json` + `@tag_ids_json` |
| `ISeriesRepository.cs:47,83` | Interface still has `string genre` param |
| `API/Contracts/CreateSeriesDraftForm.cs:23` | `string Genre` → needs `List<Guid> GenreIds` |
| `API/Contracts/UpdateSeriesDraftForm.cs:16` | Same |

### Phase 3 cleanup — SeriesProposal entity property

| File | Remaining usage |
|------|-----------------|
| `SeriesProposal.cs:13` | `string GenreSnapshot` — kept unmapped; Phase 2/3 should remove after all handlers use Series.Genres |

### Phase 5 cleanup — frontend

| File | Remaining usage |
|------|-----------------|
| `MangakaDashboard.razor` | Hardcoded `_genres` array, single-genre dropdown, `series.Genre` display, `GenreSnapshot` search |
| `SeriesPage.razor` | `_series.Genre` single-string display |
| `ProposalReviewDetail.razor` | `_proposal.GenreSnapshot` single-string display |
| `CreatorWorkspace.razor` | `series.Genre` subtitle |
| `MangakaSeriesApiClient.cs` | `string genre` param serialized as `"Genre"` form field |

## Next recommended phase

**Phase 2: Update read DTOs and EF read queries to project `Genres`/`Tags` as proper lists.**

Suggested prompt:
```
Phase 2: Update all read DTOs that still use flat string Genre/GenreSnapshot.
Replace with IReadOnlyList<GenreDto> Genres and IReadOnlyList<TagDto> Tags.
Update read handlers to Include/ThenInclude Genres/Tags.
Do NOT change write commands, SP calls, or frontend UI yet.
```

## Manual verification notes

- Runtime testing requires the database to have `manga.Genre` and `manga.Tag` seed data loaded (see `SeedData.sql`).
- `manga.usp_Series_Create` and `manga.usp_Series_UpdateProfile` expect `@genre_ids_json` (JSON array of GUIDs) — C# write paths still pass old scalar `@genre` and will fail at runtime until Phase 3.
- `manga.usp_SeriesProposal_Submit` has a known bug (INSERT column mismatch for `genre_snapshot`). Not addressed in this phase.
