# Genre/Tag Picker Stability Rollback Fix

## Branch

`feature/Mangaka` (ahead of origin by 1)

## Date

2026-06-21

## Task summary

Rolled back the broken `GenreTagSelectionField` reusable component refactor. Restored the reliable inline genre/tag picker dialogs from the HEAD commit (da4f9b7 "Partial UI fixes for genre/tag"), applied targeted fixes: checkbox tick selection, minimum genre enforcement in picker dialog, chip-based compact card display with properly visible "+N more" badge, and tag preservation on edit save.

## Architecture path

```
Blazor Web (Razor inline picker)
  -> typed Web API client (IMangakaSeriesApiClient)
  -> API controller
  -> IMediator.Send(command/query)
  -> Application handler
  -> Infrastructure repository/SP wrapper
  -> SQL Server (usp_Series_Create / usp_Series_UpdateProfile)
```

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web page | `Components/Pages/Mangaka/MangakaDashboard.razor` | Restored inline genre/tag picker dialogs (~370 lines added back). Replaced GenreTagSelectionField component usage with inline chip display + picker buttons in both Create and Edit dialogs. Fixed card genre display to show chip-based compact representation with "+N more" badge. Applied minimum genre enforcement in picker dialog. Preserved tag-saving fix in SaveDraftEditAsync. Removed unused TruncatedGenreDisplay method. |
| Web (deleted) | `Components/Shared/GenreTagSelectionField.razor` | Deleted — broken reusable component with double-toggle bug and parameter sync issues |
| Web (deleted) | `Components/Shared/GenreTagPickerDialog.razor` | Deleted — superseded generic dialog, never used |
| Docs | `docs/revision/_CURRENT_SESSION.md` | Session tracking |
| Docs | `docs/revision/Mangaka/2026-06-21-genre-tag-picker-stability-rollback-fix.md` | This handoff |

## DB/SP impact

None. No stored procedures, tables, or migrations changed.

## Bugs fixed

### 1. Checkbox double-toggle in GenreTagSelectionField
**Root cause:** The component's picker checkboxes had no `ReadOnly="true"` attribute but the parent row had `@onclick` that also toggled. Each click fired both the MudCheckBox internal toggle AND the row click toggle.

**Fix:** Restored inline picker from HEAD with `ReadOnly="true"` on checkboxes. Row `@onclick` is the sole toggle handler.

### 2. Selection tied to page position
**Root cause:** The component used `OnParametersSet` to re-initialize `_selectedGenreIds`/`_selectedTagIds` from parameters, which runs on every render. Combined with the double-toggle, selection state was unpredictable.

**Fix:** Inline picker uses `_genrePickerSelection`/`_tagPickerSelection` as staged HashSets, initialized from current `_editGenreIds`/`_newDraftGenreIds` on open. Selection is keyed by ID, not page position. Page changes and search do not affect selection state.

### 3. Card "+N more" text clipped
**Root cause:** The card used a single `<span>` with `max-width: 160px; text-overflow: ellipsis` to display genre text. When genre count exceeded visible width, the trailing "+N more" text was clipped.

**Fix:** Card now shows up to 3 genre chips (indigo style) and a separate "+N more" chip (gray style) when genres exceed 3. The "+N more" chip has a `title` attribute with the full remaining genre list. No text clipping.

### 4. Tag clearing on edit save
**Root cause:** `SaveDraftEditAsync` in HEAD overwrote `_editGenreIds` and `_editTagIds` from `result.Genres` and `result.Tags`, which are empty lists from the handler DTO. This caused tags to disappear after save.

**Fix (preserved):** SaveDraftEditAsync now builds `matchedGenreDtos` and `matchedTagDtos` from current `_editGenreIds`/`_editTagIds` + `_availableGenres`/`_availableTags` in-memory. The `_editGenreIds` and `_editTagIds` are NOT overwritten from the empty result DTO.

### 5. No minimum genre enforcement in picker
**Root cause:** HEAD picker allowed deselecting all genres and clicking Apply. The Create button was disabled when genre count was 0, but the picker itself had no guard.

**Fix:** 
- Apply button is disabled when `_genrePickerSelection.Count == 0`
- "At least one genre is required." message shown below Apply button when count is 0
- `TogglePickerGenre` silently blocks toggling off the last selected genre
- `ApplyGenrePicker` returns early if count is 0 (backup guard)

## Picker behavior (correct model)

- Opening picker copies current selected IDs into staged `HashSet<Guid>`
- Changing page or search does not change selection
- Selected state follows actual item IDs across pages/search
- Clicking a row toggles that item's ID (checkbox is ReadOnly, visual-only)
- Apply commits staged IDs to the parent selected IDs
- Cancel discards staged changes
- Minimum genre enforcement: cannot deselect all genres in picker

## Card display (new compact model)

- First 3 genres shown as indigo chips
- If more than 3 genres: "+N more" gray chip with hover tooltip
- Status badge shown after genre chips
- No text clipping, no expanding card height
- `GenreDisplay` field retained for search/filter compatibility

## Manual smoke status

Manual smoke not run; build-only verification completed.

### Smoke checklist (user must verify)

```
[ ] Open /mangaka
[ ] Series card with many genres shows first 3 chips + separate visible "+N more"
[ ] Card does not stretch/crowd action buttons
[ ] Hovering "+N more" shows tooltip with remaining genre names
[ ] Open Create Draft
[ ] Open Manage Genres
[ ] Existing/selected genres show checked checkbox with row highlight
[ ] Page 1 selection does not incorrectly select items on page 2
[ ] Search does not lose selected state
[ ] Select genre, Apply, selected genre appears in main dialog
[ ] Create button enables after selecting one genre
[ ] Cannot remove/uncheck final selected genre in picker
[ ] Open Manage Tags
[ ] Selected tags show checked checkbox with row highlight
[ ] Page/search does not mismatch tag selection
[ ] Select tag, Apply, selected tag appears in main dialog
[ ] Create Draft sends correct GenreIds/TagIds
[ ] Open Edit Draft
[ ] Existing genres are pre-selected (checkbox checked)
[ ] Existing tags are pre-selected (checkbox checked)
[ ] Save Edit without touching tags preserves tags in DB
[ ] Intentionally removing all tags clears tags in DB
```

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
0 errors, 59 warnings (all pre-existing baseline)
```

## Known issues

1. **Handler returns empty Genres/Tags**: `UpdateSeriesDraftCommandHandler` returns `new List<GenreDto>()` and `new List<TagDto>()`. The tag preservation is handled at dashboard level (in-memory preservation), but the handler DTO is still stale for any other consumer.
2. **ReferenceDataController token**: The controller does not have `[Authorize]`, see prior handoffs. Not addressed here.

## Follow-ups

1. **Handler response fix**: Modify `UpdateSeriesDraftCommandHandler` to fetch and return updated genres/tags from DB after stored procedure call.
2. **Runtime DB verification**: Tag preservation and genre save should be tested against a running database.

## Constraints confirmed

- No `SeriesGenre` C# entity created
- No `SeriesTag` C# entity created
- No `Series.genre` reintroduced
- No `SeriesProposal.genre_snapshot`/`tag_snapshot` reintroduced
- No stored procedures changed
- No DB migration created
- No `ISeriesService.GetAllSeriesAsync` reintroduced
- Architecture boundary preserved: Web → typed API client → API → MediatR → Application → Infrastructure → SQL/SP
