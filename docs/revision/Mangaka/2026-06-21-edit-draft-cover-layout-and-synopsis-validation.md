# Edit Draft Cover Layout, Preview Popup, Synopsis Height, and Synopsis Validation

## Branch

`feature/Mangaka`

## Date

2026-06-22 (corrected)

## Why runtime showed no change previously

Commit `ba7ccf9` only committed the API + handler synopsis validation (3 files, 141 lines). The MangakaDashboard.razor changes were limited to 2 lines — only `Lines="6"` on the synopsis MudTextField. The major template changes (inline cover layout, preview popup, create cover layout, synopsis validation guard, title fallback removal, popup state/methods) were **not included** in that commit. The user's runtime testing correctly showed no visible UI changes.

This session: all missing template, state, and method changes are now applied to `MangakaDashboard.razor`.

## Files changed this session

| Layer | File | Change |
|-------|------|--------|
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Edit Draft cover inline layout; Create Draft cover inline layout; cover preview popup modal; synopsis `Lines` 3→6; synopsis validation guard; removed title fallback; added popup state + OpenCoverPreview/CloseCoverPreview; added `_showCoverPreviewDialog` to `IsAnyDashboardModalOpen` |

Previously committed (ba7ccf9):
| Layer | File | Change |
|-------|------|--------|
| API | `Controllers/MangakaSeriesController.cs` | Added synopsis validation (BadRequest) |
| Application | `Features/.../UpdateSeriesDraftCommandHandler.cs` | Replaced fallback with InvalidOperationException throw |

## Cover inline layout changes

### Edit Draft modal
- **Before:** Cover preview (80×113) centered above upload zone. Vertical stack.
- **After:** Cover preview (120×180, 2:3 portrait) beside upload zone in flex row. Desktop: side-by-side. Small screens: wraps. Clickable → opens preview popup.

### Create Draft modal
- **Before:** Cover preview (80×120) centered vertically with filename below upload zone.
- **After:** Cover preview (120×180) beside upload zone in flex row. Clickable → opens preview popup.

## Cover preview popup

- MudOverlay + MudCard modal (520px max, 94vw)
- Title: "Series Cover Preview"
- Large cover display (max-height 420px, object-fit: contain)
- Close: header X button, footer Close button, click outside overlay
- Works for: edit current cover, edit replacement cover, create selected cover

## Synopsis textarea height

- `Lines` changed from `3` to `6` on `MudTextField`

## Synopsis validation fix

### Root cause
Two fallback points silently replaced empty synopsis with title text:
1. **Web UI** — `synopsis: string.IsNullOrWhiteSpace(_editSynopsis) ? _editTitle.Trim() : _editSynopsis.Trim()`
2. **Handler** — `string synopsis = string.IsNullOrWhiteSpace(command.Synopsis) ? title : command.Synopsis.Trim()`

Neither fetched the existing DB value. The "sad" reappearance was from the title fallback writing title text as synopsis.

### Fix
| Layer | Before | After |
|-------|--------|-------|
| Web UI | Title fallback in `UpdateDraftAsync` call | Synops validation guard blocks empty before API call. Sends `_editSynopsis.Trim()` only. |
| API controller | No synopsis validation | `BadRequest("Synopsis / Description is required.")` for empty synopsis |
| Handler | Title fallback | `throw new InvalidOperationException("Synopsis / Description is required.")` |

## Backend/API/DB/SP impact

None. No DB schema, SP, Cloudinary, or FileResource changes.

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
0 errors, 57 warnings (all pre-existing baseline)
0 new changed-file warnings
```

## Manual smoke

Runtime smoke not run; user must verify manually.

```
[ ] Open Edit Draft
[ ] Current cover appears inline beside "Click to replace cover" (120×180)
[ ] Click current cover → preview popup opens with large image
[ ] Close preview popup (X, Close button, click outside)
[ ] Select replacement cover → crop dialog → confirm
[ ] Replacement cover appears inline beside upload zone (120×180)
[ ] Click replacement preview → preview popup opens
[ ] Save edit — cropped 1000×1500 PNG uploaded
[ ] Open Create Draft
[ ] Select/crop cover → preview appears inline (120×180)
[ ] Click preview → popup opens
[ ] Synopsis textarea is larger (6 lines)
[ ] Empty synopsis → "Synopsis / Description is required." snackbar
[ ] Edit modal stays open on validation error
[ ] Non-empty synopsis persists correctly after save/reopen
[ ] API rejects empty synopsis with 400
[ ] Handler throws InvalidOperationException for empty synopsis
[ ] No silent fallback to title or old synopsis
```

## Restart instructions

```powershell
dotnet clean MangaManagementSystem/MangaManagementSystem.sln
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
dotnet run --project MangaManagementSystem/src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj
```

Browser: `Ctrl + F5` (hard refresh)
