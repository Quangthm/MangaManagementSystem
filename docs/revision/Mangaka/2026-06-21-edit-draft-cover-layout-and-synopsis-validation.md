# Edit Draft Cover Layout, Preview Popup, Synopsis Height, and Synopsis Validation

## Branch

`feature/Mangaka`

## Date

2026-06-21

## Task summary

Four improvements in one session:
1. **Edit Draft cover inline layout** — Moved cover preview beside the upload zone instead of above it
2. **Clickable cover preview popup** — Added a full-size preview modal when clicking any cover preview
3. **Synopsis textarea height** — Increased from 3 lines to 6 lines
4. **Synopsis validation bug** — Removed fallback logic that silently replaced empty synopsis with title text, now properly blocks empty synopsis with validation

## Root cause of synopsis fallback bug

Two fallback locations were found:

| Layer | File | Line | Old behavior |
|-------|------|------|-------------|
| **Web UI** | `MangakaDashboard.razor` | 2253 | `synopsis: string.IsNullOrWhiteSpace(_editSynopsis) ? _editTitle.Trim() : _editSynopsis.Trim()` — sent title as synopsis when user cleared the field |
| **Handler** | `UpdateSeriesDraftCommandHandler.cs` | 83-85 | `string synopsis = string.IsNullOrWhiteSpace(command.Synopsis) ? title : command.Synopsis.Trim()` — double fallback to title |

**No database lookup fallback existed.** The handler never fetches the existing series before update. The "sad" reappearance was caused by the title fallback: when synopsis was empty, title was sent as synopsis, so the DB stored the title text. However, after the UI card refresh succeeded with the fresh DB data, the synopsis displayed correctly. The bug behavior ("sad" came back) likely resulted from a card refresh error keeping the stale card, or the UI `_editSynopsis` being repopulated from the stale `_detailSeries.Synopsis` in `OpenDraftDetails`.

The fix enforces synopsis validation at all three layers.

## Cover inline layout changes

### Edit Draft modal (before → after)

**Before:** Vertical layout
- Cover preview above upload zone, centered, small (80×113px)

**After:** Inline layout
- Cover preview (120×180px) beside upload zone in a flex row
- Clickable preview opens larger popup
- Upload zone fills remaining space
- Responsive: wraps on narrow screens (`flex-wrap: wrap`)

### Create Draft modal
- Same inline layout applied
- Selected/cropped cover preview (120×180px) beside upload zone
- Clickable preview opens same popup

## Cover preview popup

- MudOverlay + MudCard modal (520px max, 94vw)
- Title: "Series Cover Preview"
- Large cover display (max-height 420px, object-fit: contain, 2:3 ratio preserved)
- Close button in header + filled Close button in footer
- Click outside overlay dismisses
- Works for all cover previews: current cover, replacement cover, create draft cover

## Synopsis textarea height

- `Lines` changed from `3` to `6` on `MudTextField`
- Modal body scrolls if content overflows

## Synopsis validation changes by layer

### Web UI (`MangakaDashboard.razor`)
- Added guard in `SaveDraftEditAsync`: `if (string.IsNullOrWhiteSpace(_editSynopsis)) { Snackbar.Add("Synopsis / Description is required.", Severity.Warning); return; }`
- Removed title fallback: `synopsis: _editSynopsis.Trim()` instead of `synopsis: string.IsNullOrWhiteSpace(_editSynopsis) ? _editTitle.Trim() : _editSynopsis.Trim()`

### API controller (`MangakaSeriesController.cs`)
- Added: `if (string.IsNullOrWhiteSpace(request.Synopsis)) { return BadRequest(new ApiErrorResponse("Synopsis / Description is required.")); }`

### Application handler (`UpdateSeriesDraftCommandHandler.cs`)
- Replaced fallback `string synopsis = string.IsNullOrWhiteSpace(...) ? title : ...` with validation throw: `if (string.IsNullOrWhiteSpace(command.Synopsis)) throw new InvalidOperationException("Synopsis / Description is required.");`

### SQL / stored procedure
- No changes. `usp_Series_UpdateProfile` remains the final defense.

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Inline cover layout for create/edit; cover preview popup; synopsis `Lines` 3→6; synopsis validation guard; removed title fallback |
| API | `Controllers/MangakaSeriesController.cs` | Added synopsis validation before command dispatch |
| Application | `Features/Mangaka/Series/Commands/UpdateSeriesDraft/UpdateSeriesDraftCommandHandler.cs` | Replaced title fallback with validation throw |

## Backend/API/DB/SP impact

- No database schema changes
- No stored procedure changes
- No FileResource schema changes
- No Cloudinary changes
- No API contract signature changes (validation added inside existing endpoint)
- Architecture remains: Web → API → MediatR → Infrastructure → SQL

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
Build succeeded.
0 errors
57 warnings (all pre-existing baseline)
0 new changed-file warnings
```

## Manual smoke

Runtime smoke not run; user must verify manually.

```
[ ] Open Edit Draft
[ ] Current cover appears inline beside "Click to replace cover" (120x180)
[ ] Click current cover → preview popup opens with large image
[ ] Close preview popup works (X button, Close button, click outside)
[ ] Select replacement cover → crop dialog → confirm
[ ] Replacement cover appears inline beside upload zone (120x180)
[ ] Click replacement preview → preview popup opens
[ ] Save edit — cropped 1000x1500 PNG uploaded
[ ] Open Create Draft
[ ] Select/crop cover → preview appears inline (120x180)
[ ] Click preview → popup opens
[ ] Synopsis textarea is larger (6 lines)
[ ] Empty synopsis on edit is blocked with snackbar "Synopsis / Description is required."
[ ] Edit modal stays open on validation error
[ ] Non-empty synopsis persists correctly after save/reopen
[ ] API rejects empty synopsis with 400 if sent directly
[ ] Handler throws InvalidOperationException for empty synopsis
[ ] No silent fallback to title or old synopsis value
```
