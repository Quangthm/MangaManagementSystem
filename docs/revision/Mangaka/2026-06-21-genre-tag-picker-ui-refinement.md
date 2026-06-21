# Genre/Tag Picker UI Refinement (Phase 4.5)

**Branch:** `feature/Mangaka`
**Date:** 2026-06-21
**Scope:** Replace checkbox lists with chip-based selected-list patterns + picker popups with search/pagination
**Related workflow:** BF-SERIES-001, BF-SERIES-002

## Task summary

Replaced the tall multi-select checkbox lists in the MangakaDashboard create/edit draft dialogs with compact chip-based selected-item displays and separate picker popup dialogs. Genres and Tags now show as removable chips under their section headers, with an "Add / Manage" button that opens a dedicated picker dialog supporting search-by-name-or-description and pagination.

## Architecture path

### Pickers consume existing reference data (read):

```
MangakaDashboard
  → ReferenceDataApiClient (unchanged)
  → GET /api/reference/genres, /api/reference/tags (unchanged)
```

### DTOs extended:

`GenreDto` and `TagDto` gained an optional `string? Description` property to support search-by-description in the picker. The domain entities already had `Description` — now it surfaces to the UI.

## Files changed

### Application DTOs (2 files)

| File | Change |
|------|--------|
| `Application/DTOs/Manga/GenreDto.cs` | Added `string? Description = null` |
| `Application/DTOs/Manga/TagDto.cs` | Added `string? Description = null` |

### Application handlers (2 files)

| File | Change |
|------|--------|
| `Features/ReferenceData/Queries/GetGenres/GetGenresQueryHandler.cs` | Mapped `g.Description` → `GenreDto` |
| `Features/ReferenceData/Queries/GetTags/GetTagsQueryHandler.cs` | Mapped `t.Description` → `TagDto` |

### Web UI (1 file)

| File | Change |
|------|--------|
| `Web/Components/Pages/Mangaka/MangakaDashboard.razor` | Replaced both create/edit checkbox lists with chip display + picker dialogs; added picker state fields and methods |

## UI behavior

### Create/Edit Dialogs — Genre section
- Selected genres shown as indigo chips (`#eef2ff` bg, `#4f46e5` text) with `×` remove buttons
- "Add / Manage Genres" outlined button opens the genre picker
- Removing the last genre shows Snackbar warning "At least one genre is required."
- Create/Save button remains disabled when no genre selected

### Create/Edit Dialogs — Tag section
- Selected tags shown as green chips (`#f0fdf4` bg, `#059669` text) with `×` remove buttons
- "Add / Manage Tags" outlined button opens the tag picker
- Tags are optional; removing all tags is allowed

### Genre Picker Dialog
- Title "Manage Genres" with close button
- Search text field (by name or description, case-insensitive, Immediate)
- Checkbox list with name + description subtitle (when available)
- Pagination: 8 items per page, Prev/Next buttons, page indicator
- Cancel discards changes; Apply copies working selection to the target HashSet
- Opens with a working copy of the current selection (either create or edit)

### Tag Picker Dialog
- Title "Manage Tags" with close button
- Same pattern as genre picker
- Pagination: 10 items per page
- Tag chips use green color scheme to visually distinguish from genre chips

### Implementation details
- Each picker maintains its own search, page, and working-selection state
- `_genrePickerForEdit` / `_tagPickerForEdit` flags track whether picker targets create or edit HashSet
- `TogglePickerGenre` / `TogglePickerTag` use `HashSet.Remove` + `Add` pattern (toggle behavior)
- `Apply` copies working selection to the target set and closes the picker
- `Cancel` closes without copying
- Old `ToggleId` static method removed (replaced by picker-specific toggle methods)

## New/Changed code-behind properties

| Property | Type | Purpose |
|----------|------|---------|
| `_genrePickerOpen`/`_tagPickerOpen` | `bool` | Picker visibility |
| `_genrePickerForEdit`/`_tagPickerForEdit` | `bool` | Target mode |
| `_genrePickerSearch`/`_tagPickerSearch` | `string` | Search text |
| `_genrePickerPage`/`_tagPickerPage` | `int` | Current page |
| `GenrePickerPageSize`/`TagPickerPageSize` | `const int` | 8 / 10 |
| `_genrePickerSelection`/`_tagPickerSelection` | `HashSet<Guid>` | Working copy |
| `FilteredPickerGenres`/`FilteredPickerTags` | computed | Search-filtered + sorted |
| `GenrePickerTotalPages`/`TagPickerTotalPages` | computed | Page count |
| `PagedPickerGenres`/`PagedPickerTags` | computed | Current page items |

## Search behavior
- Searches both `Name` and `Description` (case-insensitive `Contains`)
- Empty search shows all items
- Filter + pagination are entirely client-side (reference data is small)

## Build result

```
dotnet build --no-incremental
0 errors, 65 warnings
```

No new warnings. All warnings pre-existing MudBlazor/CS8602.

## Confirmed constraints
- No DB schema changes, no SQL tables created, no migrations
- No SeriesGenre/SeriesTag C# entities created
- No old genre/tag snapshot fields reintroduced
- No changes to API controllers, MediatR commands/queries (except Description mapping), or infrastructure
- Works with existing reference data endpoints
- Minimum 1 genre requirement preserved
- Tags remain optional

## Manual verification checklist
1. [x] Build succeeds with 0 errors
2. [ ] Open create dialog — genre chips show "No genres selected"; tag chips show "No tags selected"
3. [ ] Click "Add / Manage Genres" — genre picker opens with search and pagination
4. [ ] Select genres in picker, click Apply — chips appear in create dialog
5. [ ] Click × on a genre chip — it's removed; removing last shows warning
6. [ ] Create button disabled when no genres selected
7. [ ] Click "Add / Manage Tags" — tag picker opens similarly
8. [ ] Remove all tags — allowed (no warning)
9. [ ] Edit dialog — pre-populated with existing genre/tag chips
10. [ ] Edit dialog — picker pre-selects existing genres/tags
11. [ ] Search in picker filters by name and description
12. [ ] Pagination works correctly in both pickers
13. [ ] Cancel in picker discards changes
14. [ ] Create/edit workflow continues to pass real GenreIds/TagIds
