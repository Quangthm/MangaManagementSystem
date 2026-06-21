# Fix Phase 4 ReferenceDataRepository DI Error

**Branch:** `feature/Mangaka`
**Date:** 2026-06-21

## Task summary

Fixed startup DI error where `ReferenceDataRepository` could not be constructed because it injected `IApplicationDbContext`, which is not registered in the project's DI container. All existing repositories inject the concrete `ApplicationDbContext`, not the interface.

## Architecture path

Reference data flow unchanged:

```
MangakaDashboard
  → ReferenceDataApiClient
  → GET /api/reference/genres, /api/reference/tags
  → ReferenceDataController
  → MediatR GetGenresQuery / GetTagsQuery
  → Application handler
  → IReferenceDataRepository.GetGenresAsync / GetTagsAsync
  → Infrastructure ReferenceDataRepository (ApplicationDbContext, EF AsNoTracking)
  → SQL Server
```

## Root cause

`ReferenceDataRepository` constructor took `IApplicationDbContext` as a parameter, but:
- The DI container registers `ApplicationDbContext` (concrete class) via `AddDbContext<ApplicationDbContext>`
- `IApplicationDbContext` is never registered in DI
- The interface exists but is used as an internal abstraction, not a DI-registered service
- All existing repos (`GenericRepository<T>`, `SeriesRepository`, etc.) inject the concrete `ApplicationDbContext`

## Files changed

| File | Change |
|------|--------|
| `Infrastructure/Repositories/ReferenceDataRepository.cs` | Constructor: `IApplicationDbContext` → `ApplicationDbContext` |

No duplicate DI registration was found. The aggregate exception message listing `IReferenceDataRepository` multiple times is normal behavior when the DI container tries multiple resolution paths for the same dependency chain.

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
0 errors, 65 warnings
```

Within baseline range. No new warnings from changed files.

## Legacy reference audit

### Remaining GenreSnapshot references (4, all transitional/legacy)

| File | Line | Status |
|------|------|--------|
| `Domain/Entities/SeriesProposal.cs` | 13 | Unmapped entity property (`.Ignore()` in EF config) |
| `Infrastructure/Persistence/Configurations/SeriesProposalConfiguration.cs` | 14 | `builder.Ignore(sp => sp.GenreSnapshot)` — intentional |
| `Application/DTOs/Manga/SeriesProposalDtos.cs` | 63 | `CreateProposalDto.GenreSnapshot` — legacy write DTO |
| `Application/Services/SeriesProposalService.cs` | 78 | `GenreSnapshot = dto.GenreSnapshot` — legacy service |

### @genre in C# write calls (0)

No remaining `@genre` references in any C# create/update draft write path.

### genre_snapshot in SQL (0)

No `genre_snapshot` references in SQL schema or stored procedure files. `usp_SeriesProposal_Submit` INSERT column list was already corrected — no `genre_snapshot` column remains.

### _genres / _newDraftGenre / _editGenre (0 remaining as issues)

All replaced with `_availableGenres`/`_availableTags` from API and `HashSet<Guid>` selections.

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

- [ ] App starts without `System.AggregateException` DI error
- [ ] `GET /api/reference/genres` returns genre list
- [ ] `GET /api/reference/tags` returns tag list
- [ ] `/mangaka` loads without crash
- [ ] Create Draft dialog shows genre/tag checkboxes
- [ ] Edit Draft dialog opens with pre-selected genres/tags

## Known issues

None from this fix. Runtime verification not performed — build-only.
