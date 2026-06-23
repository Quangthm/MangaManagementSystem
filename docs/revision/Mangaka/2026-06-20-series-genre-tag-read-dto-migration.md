# Series Genre/Tag Read DTO Migration (Phase 2)

**Branch:** `feature/Mangaka`
**Date:** 2026-06-20
**Scope:** Read DTOs + query handlers for normalized Series genres/tags
**Related workflow:** Mangaka dashboard, Series detail, Proposal tracking, Editor proposals

## Task summary

Phase 2 of genre/tag normalization. Replaced all read-side flat `string Genre` / `string GenreSnapshot` DTO properties with `IReadOnlyList<GenreDto>` and `IReadOnlyList<TagDto>` lists. Updated all CQRS query handlers and legacy service mappers to project genres/tags from the Series navigation properties. Added EF Core `.Include()` for `Genres` and `Tags` to all read repository methods. Made minimal Web compile fixes to keep the build green.

## Architecture path

```
MangakaDashboard / SeriesPage / ProposalReviewDetail / CreatorWorkspace
  → typed API client (unchanged)
  → API controller (unchanged)
  → MediatR query handler (updated: MapGenres/MapTags)
  → EF AsNoTracking Include(s => s.Genres).Include(s => s.Tags)
  → SQL Server
```

## Files changed

### Application DTOs (4 files)

| File | Change |
|------|--------|
| `DTOs/Manga/SeriesDtos.cs` | `SeriesDto.Genre:string` → `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>`. Left `CreateSeriesDto`/`UpdateSeriesDto` untouched (Phase 3 write) |
| `DTOs/Manga/SeriesDetailDtos.cs` | `SeriesDetailDto.Genre:string` → `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |
| `DTOs/Manga/SeriesDraftUpdatedDto.cs` | `Genre:string` → `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |
| `DTOs/Manga/SeriesProposalDtos.cs` | Replaced `GenreSnapshot:string` with `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` in: `SeriesProposalDto`, `ProposalQueueItemDto`, `EditorProposalDetailDto`, `MangakaSeriesProposalDto`. Left `CreateProposalDto` untouched (Phase 3 write) |

### Application handlers/services (7 files)

| File | Change |
|------|--------|
| `Features/Mangaka/Series/Queries/GetMyMangakaSeries/GetMyMangakaSeriesQueryHandler.cs` | Added `MapGenres()`/`MapTags()` helpers; DTO maps genre/tag lists |
| `Features/Series/Queries/GetSeriesBySlug/GetSeriesBySlugQueryHandler.cs` | Same |
| `Features/Mangaka/SeriesProposals/Queries/GetMySeriesProposals/GetMySeriesProposalsQueryHandler.cs` | Resolves genres/tags from `proposal.Series.Genres`/`Tags` |
| `Features/Editor/SeriesProposals/Queries/GetEditorialProposalQueue/GetEditorialProposalQueueQueryHandler.cs` | Resolves from `p.Series.Genres`/`Tags` |
| `Features/Editor/SeriesProposals/Queries/GetEditorProposalDetail/GetEditorProposalDetailQueryHandler.cs` | Resolves from `proposal.Series.Genres`/`Tags` |
| `Services/SeriesService.cs` | `MapToDto` uses `MapGenres/MapTags` helpers |
| `Services/SeriesProposalService.cs` | `GetEditorialQueueAsync` and `MapToDto` resolve from `Series.Genres`/`Tags` |
| `Features/Mangaka/Series/Commands/UpdateSeriesDraft/UpdateSeriesDraftCommandHandler.cs` | `Genre` → empty `Genres`/`Tags` lists (Phase 3 will populate properly) |

### Infrastructure repositories (2 files)

| File | Method | Change |
|------|--------|--------|
| `Repositories/SeriesRepository.cs` | `GetByActiveContributorWithCoverAsync` | Added `.Include(s => s.Genres).Include(s => s.Tags)` |
| `Repositories/SeriesRepository.cs` | `GetAllWithCoverAsync` | Added `.Include(s => s.Genres).Include(s => s.Tags)` |
| `Repositories/SeriesRepository.cs` | `GetSeriesDetailBySlugAsync` | Added `.Include(s => s.Genres).Include(s => s.Tags)` |
| `Repositories/SeriesRepository.cs` | `GetSeriesWithChaptersAsync` | Added `.Include(s => s.Genres).Include(s => s.Tags)` |
| `Repositories/SeriesProposalRepository.cs` | `GetByIdWithDetailsAsync` | Added `.Include(sp => sp.Series!).ThenInclude(s => s.Genres)` and same for Tags |
| `Repositories/SeriesProposalRepository.cs` | `GetEditorialQueueAsync` | Changed `.Include(sp => sp.Series)` to `.Include(sp => sp.Series).ThenInclude(s => s.Genres)` + Tags |
| `Repositories/SeriesProposalRepository.cs` | `GetMySeriesProposalsAsync` | Same |

### Web (4 files)

| File | Change |
|------|--------|
| `Components/Pages/Mangaka/MangakaDashboard.razor` | `series.Genre` → `series.GenreDisplay`; `_selectedProposal.GenreSnapshot` → `string.Join(", ", ...Genres...)`; search filters use `Genres.Any()` / `GenreDisplay.Contains()`; `SeriesCardData` record field `Genre:string` → `GenreDisplay:string`; edit-draft update reads from `result.Genres` |
| `Components/Pages/Series/SeriesPage.razor` | Genre display: single span → foreach chip loop over `_series.Genres` |
| `Components/Pages/Editor/ProposalReviewDetail.razor` | `_proposal.GenreSnapshot` → `string.Join(", ", _proposal.Genres.Select(...))` |
| `Components/Pages/Mangaka/CreatorWorkspace.razor` | `series.Genre` → `string.Join(", ", series.Genres.Select(...))` |

## DTO contract changes summary

| Old property | New properties |
|-------------|---------------|
| `SeriesDto.Genre:string` | `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |
| `SeriesDetailDto.Genre:string` | `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |
| `SeriesDraftUpdatedDto.Genre:string` | `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |
| `SeriesProposalDto.GenreSnapshot:string` | `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |
| `ProposalQueueItemDto.GenreSnapshot:string` | `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |
| `EditorProposalDetailDto.GenreSnapshot:string` | `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |
| `MangakaSeriesProposalDto.GenreSnapshot:string` | `Genres:IReadOnlyList<GenreDto>, Tags:IReadOnlyList<TagDto>` |

## Read query mapping summary

| Screen | Genres/Tags source | EF includes |
|--------|-------------------|-------------|
| Mangaka dashboard | `Series.Genres` / `Series.Tags` (via `GetByActiveContributorWithCoverAsync`) | `.Include(s => s.Genres).Include(s => s.Tags)` |
| Series detail page | `series.Genres` / `series.Tags` (via `GetSeriesDetailBySlugAsync`) | Same |
| Mangaka proposal tracking | `proposal.Series.Genres` / `Tags` (via `GetMySeriesProposalsAsync`) | `.Include(sp => sp.Series).ThenInclude(s => s.Genres)` |
| Editor proposal queue | `p.Series.Genres` / `Tags` (via `GetEditorialQueueAsync`) | Same |
| Editor proposal detail | `proposal.Series.Genres` / `Tags` (via `GetByIdWithDetailsAsync`) | Same |
| Editor series library | N/A (DTO doesn't have genre/tag yet) | — |
| CreatorWorkspace | `series.Genres` → joined to string for subtitle | Via GetByActiveContributorWithCoverAsync |

## Remaining legacy genre usage

### Phase 3 write workflow cleanup

| File | Remaining usage |
|------|-----------------|
| `CreateSeriesDraftCommand.cs:21` | `string Genre` → needs `IReadOnlyList<Guid> GenreIds` |
| `UpdateSeriesDraftCommand.cs:21` | Same |
| `CreateSeriesDraftDto.cs:15` | `string Genre` input DTO |
| `CreateProposalDto.cs:60` | `string GenreSnapshot` input DTO |
| `SeriesRepository.cs:83,104,187,208` | Passes `@genre` scalar → needs `@genre_ids_json` + `@tag_ids_json` |
| `ISeriesRepository.cs:47,83` | Interface still has `string genre` param |
| `API/Contracts/CreateSeriesDraftForm.cs:23` | `string Genre` → needs `List<Guid> GenreIds` |
| `API/Contracts/UpdateSeriesDraftForm.cs:16` | Same |
| `MangakaSeriesApiClient.cs:69,195` | Sends `"Genre"` form field |
| `SeriesService.cs:76,134` | Validates/passes `string genre` (transitional) |

### Phase 4 frontend create/edit UI

| File | Remaining usage |
|------|-----------------|
| `MangakaDashboard.razor` | Hardcoded `_genres` array (8 values), single-genre MudSelect dropdown, `_newDraftGenre` / `_editGenre` string fields |

### Known transitional (unmapped entity property)

| File | Remaining usage |
|------|-----------------|
| `SeriesProposal.cs:13` | `string GenreSnapshot` — kept unmapped in EF. Still used by `SeriesProposalService.CreateProposalAsync` (transitional). Phase 3 will remove. |

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
- No write workflow changed (except compile fix in UpdateSeriesDraftCommandHandler).
- No create/edit draft UI redesign done.

## Manual verification checklist

1. [ ] Mangaka dashboard loads series cards with genre/tag chips (from `SeriesCardData.GenreDisplay`)
2. [ ] Series detail page (`/series/{slug}`) shows multiple genre chips
3. [ ] Mangaka proposal tracking shows genres from series metadata
4. [ ] Editor proposal queue shows genres from series metadata
5. [ ] Editor proposal detail shows genres from series metadata
6. [ ] Creator workspace subtitle shows joined genre names
7. [ ] Build succeeds with 0 errors

## Next recommended prompt

Phase 3: Update create/edit draft write workflow to send `GenreIds`/`TagIds` JSON arrays to `manga.usp_Series_Create` and `manga.usp_Series_UpdateProfile`. Fix `ISeriesRepository` interface, `SeriesRepository` ADO.NET SP calls, commands, command handlers, API contracts, and typed API client.
