# Genre/Tag Picker Component and Update Fix

## Branch

`feature/Mangaka` (ahead of origin by 1 commit)

## Date

2026-06-21

## Task summary

Replaced the inline genre/tag picker dialogs in MangakaDashboard with a reusable `GenreTagSelectionField` component. Fixed checkbox tick selection (used row-darkening only), added compact chip display with bounded height, fixed the tag-clearing bug in `SaveDraftEditAsync`, and cleaned up ~370 lines of old inline picker code from the dashboard.

## Problem fixed

1. **Checkbox selection**: The picker relied on row darkening alone; now each row has a `MudCheckBox` displaying a visible tick when selected.
2. **Compact chip display**: Selected items are shown as compact colored chips with `max-height: 80px; overflow-y: auto` and a "+N more" overflow indicator when chip count exceeds `MaxVisibleChips` (default 6).
3. **Reusable component**: The inline picker UI (search, pagination, selection) was duplicated for genres and tags in `MangakaDashboard`. Extracted into `GenreTagSelectionField`, usable by both create and edit dialogs.
4. **Tag-clearing bug**: `SaveDraftEditAsync` overwrote `_editGenreIds`/`_editTagIds` from the API response DTO, which returns empty `Genres`/`Tags` lists. Fixed by preserving the current in-memory selection and building card display data from `_editGenreIds`/`_editTagIds` + `_availableGenres`/`_availableTags`.

## Architecture path

### GenreTagSelectionField data flow

```
MangakaDashboard
  → GenreTagSelectionField (reusable Web component)
  → [bind: SelectedGenreIds, SelectedTagIds]
  → internal HashSet<Guid> for staged selection
  → EventCallback<HashSet<Guid>> notifies parent
```

### Reference data (read, unchanged from Phase 4)

```
MangakaDashboard
  → ReferenceDataApiClient
  → GET /api/reference/genres, /api/reference/tags
  → ReferenceDataController
  → MediatR GetGenresQuery / GetTagsQuery
  → Application handler
  → IReferenceDataRepository
  → ReferenceDataRepository (EF AsNoTracking)
  → SQL Server
```

### Create/Edit draft (write, unchanged from Phase 3/4)

```
MangakaDashboard
  → MangakaSeriesApiClient
  → MangakaSeriesController
  → MediatR UpdateSeriesDraftCommand / CreateSeriesDraftCommand
  → Application handler
  → ISeriesRepository
  → SeriesRepository
  → manga.usp_Series_Create / manga.usp_Series_UpdateProfile
```

## Files changed

### New file

| Layer | File | Change |
|-------|------|--------|
| Web shared component | `Components/Shared/GenreTagSelectionField.razor` | **New** — Reusable genre/tag chip display + picker dialog component with search, pagination, staged selection, and two-way binding |

### Modified file

| Layer | File | Change |
|-------|------|--------|
| Web page | `Components/Pages/Mangaka/MangakaDashboard.razor` | Replaced inline genre/tag chip sections and picker dialogs with `<GenreTagSelectionField>`; removed ~370 lines of old picker code; fixed tag-clearing bug in `SaveDraftEditAsync` |

### Unused file (untracked, superseded)

| Layer | File | Change |
|-------|------|--------|
| Web shared component | `Components/Shared/GenreTagPickerDialog.razor` | **Superseded** — Was created and then replaced by `GenreTagSelectionField`. Left in working tree for reference. |

## Component API

`GenreTagSelectionField` parameters:

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `AvailableGenres` | `IReadOnlyList<GenreDto>` | required | Reference list of all genres |
| `AvailableTags` | `IReadOnlyList<TagDto>` | required | Reference list of all tags |
| `SelectedGenreIds` | `HashSet<Guid>` | `new()` | Two-way bound genre selection |
| `SelectedGenreIdsChanged` | `EventCallback<HashSet<Guid>>` | — | Notifies parent on genre change |
| `SelectedTagIds` | `HashSet<Guid>` | `new()` | Two-way bound tag selection |
| `SelectedTagIdsChanged` | `EventCallback<HashSet<Guid>>` | — | Notifies parent on tag change |
| `RequireGenre` | `bool` | `true` | When true, enforces minimum 1 genre |
| `Disabled` | `bool` | `false` | Disables remove buttons |
| `GenrePageSize` | `int` | `8` | Pagination page size for genre picker |
| `TagPageSize` | `int` | `10` | Pagination page size for tag picker |
| `MaxVisibleChips` | `int` | `6` | Max chips shown before "+N more" overflow |

## UI behavior changed

### Checkbox tick selected state
- Each picker row has `MudCheckBox ReadOnly="true"` reflecting the `sel` state — **confirmed** (lines 104, 177 of `GenreTagSelectionField.razor`)

### Selected chips / compact display
- Selected items shown as removable colored chips (indigo for genres, green for tags)
- Container has `max-height: 80px; overflow-y: auto` to bound height
- When chips exceed `MaxVisibleChips`, a "+N more" overflow indicator is shown

### X remove behavior
- Genre removal from chip: blocked when `RequireGenre` is true and only 1 genre remains; shows `Snackbar` warning
- Tag removal: always allowed (tags are optional)

### Minimum genre enforcement
- Apply button in genre picker: disabled when `RequireGenre` && selection count is 0
- `ToggleGenrePickerItem`: silently blocks toggling off the last genre when `RequireGenre`
- `ApplyGenrePicker`: returns early if `RequireGenre` && count is 0
- `RemoveGenre`: shows snackbar warning when `RequireGenre` && count <= 1

### Search and pagination
- Search filters by name and description (case-insensitive `Contains`)
- Empty search shows all items
- Pagination resets to page 1 on search change
- Genre page size: 8; Tag page size: 10

### Card / dialog overflow behavior
- Series card genre display truncates after 3 genres with "+N more" suffix and `max-width: 160px; text-overflow: ellipsis`
- Edit dialog uses `GenreTagSelectionField` which has bounded `max-height: 80px` for chips

## Update tag preservation

**Actual final behavior (code inspection):**

- Edit opens with existing genres selected: `OpenDraftDetails` sets `_editGenreIds = new HashSet<Guid>(series.Genres.Select(g => g.GenreId))` — **confirmed** (line 1428)
- Edit opens with existing tags selected: `OpenDraftDetails` sets `_editTagIds = new HashSet<Guid>(series.Tags.Select(t => t.TagId))` — **confirmed** (line 1429)
- Save sends current selected `GenreIds` and `TagIds` — **confirmed** (lines 1524-1525)
- After save, `_editGenreIds`/`_editTagIds` are preserved from current in-memory state, NOT overwritten from the handler's empty response DTO — **confirmed** (comment at line 1556, removed overwrite lines)
- Card `Genres`/`Tags` are rebuilt from `_editGenreIds`/`_editTagIds` + `_availableGenres`/`_availableTags` in-memory, NOT from empty `result.Genres`/`result.Tags` — **confirmed** (lines 1541-1551)

**Build/code verification only; DB runtime verification not run.**

## DB/SP impact

- No SQL tables created.
- No migrations created.
- No stored procedures changed.
- Existing procedure behavior: `usp_Series_UpdateProfile` replaces `SeriesGenre` and `SeriesTag` with the IDs passed by the UI/API. The stored procedure writes the correct data to the DB; only the response DTO returns empty lists.

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
0 errors, 60 warnings
```

All warnings are pre-existing baselines (MudBlazor MUD0002, CS8602 null dereference in Infrastructure, CS0108 `_context` hiding, CS0649 `_isLoading`). No new warnings from changed files.

## Manual smoke status

Manual smoke not run; build-only verification completed.

Checklist:

```
[ ] Open /mangaka
[ ] Open Create Draft dialog
[ ] Main modal remains compact
[ ] Manage Genres opens
[ ] Selected genres show checkbox tick
[ ] Search/pagination works
[ ] Cannot remove final genre
[ ] Manage Tags opens
[ ] Selected tags show checkbox tick
[ ] Edit Draft pre-selects existing genres/tags
[ ] Save Edit preserves unchanged tags
[ ] Series card remains compact
```

## Known issues

1. **Handler returns empty Genres/Tags**: `UpdateSeriesDraftCommandHandler` constructs `SeriesDraftUpdatedDto` with `Genres = new List<GenreDto>()` and `Tags = new List<TagDto>()`. The stored procedure updates the DB correctly, but the response DTO omits the updated genre/tag data. The tag-clearing bug is fixed at the dashboard level (in-memory preservation), but the handler still returns stale data. If another consumer uses the same response DTO, they would see empty genres/tags.
2. **GenreTagPickerDialog.razor untracked**: The superseded file is left in the working tree. It can be deleted after review.
3. **BL0007 warning fixed**: The auto-property conversion fixed the BL0007 warnings; none remain.

## Follow-ups

1. **Handler response**: Consider modifying `UpdateSeriesDraftCommandHandler` to fetch and return the updated genres/tags from the DB after the stored procedure call, instead of returning empty lists.
2. **Runtime DB verification**: The tag-preservation fix has not been verified against a running database. The card display rebuild from `_editGenreIds`/`_editTagIds` + reference data is logically correct but should be tested end-to-end.
3. **GenreTagSelectionField unit tests**: No tests exist for the component's behavior (minimum genre, chip overflow, staged selection).
4. **Legacy GenreSnapshot**: Remaining transitional references documented in previous handoffs (unrelated to this task).

## Constraints confirmed

- No `SeriesGenre` C# entity created.
- No `SeriesTag` C# entity created.
- No old `Series.genre` reintroduced.
- No `SeriesProposal.genre_snapshot`/`tag_snapshot` reintroduced.
- No unrelated workflow changes.
- No `/mangaka/workspace/{SeriesId}` restoration.
- No `ISeriesService.GetAllSeriesAsync` reintroduction.
- Build architecture path preserved: Web → typed API client → API → MediatR → Application → Infrastructure → SQL/SP.
