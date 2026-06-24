# Series Cover Crop Preview and Stricter Card Refresh

## Branch

`feature/Mangaka`

## Date

2026-06-21

## Task summary

Two improvements:
1. **Stricter targeted single-card refresh fallback** — Removed the loose fallback that fabricated a card from command result + local selections when `GetMySeriesCardByIdAsync` returned null. Now: 404 removes the card from the list with a warning; network/refresh errors keep the existing card with a warning. No fabricated data.
2. **Series cover manual crop preview** — Added MVP-friendly manual 2:3 portrait crop for both create and edit draft modals, using a new reusable `ImageCropDialog.razor` component backed by `image-crop.js`.

## Architecture path

### Stricter card refresh

```
Blazor Web (MangakaDashboard.razor / SaveDraftEditAsync)
  → IMangakaSeriesApiClient.UpdateDraftAsync (command — minimal result)
  → Then: IMangakaSeriesApiClient.GetMySeriesCardByIdAsync (targeted query)
  → Success: replace card in _seriesData
  → 404: remove card, warn user
  → Error: keep existing card, warn user
```

### Cover crop (Web-only)

```
Blazor Web (ImageCropDialog.razor)
  → JS interop (image-crop.js — canvas-based crop)
  → User confirms crop → bytes returned to parent
  → Parent stores cropped bytes for existing multipart upload flow
  → Backend API/MediatR/Cloudinary/SP — unchanged
```

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Stricter fallback in `SaveDraftEditAsync`; integrated cover crop into create/edit modals; added crop state fields and handlers |
| Web (new) | `Components/Shared/ImageCropDialog.razor` | New reusable crop dialog component with configurable aspect ratio, zoom, drag, confirm/cancel |
| Web (new) | `wwwroot/js/image-crop.js` | New JS module — canvas-based image cropper with configurable aspect ratio (based on avatar cropper pattern) |
| Web | `Components/App.razor` | Added `<script src="/js/image-crop.js">` reference |
| Docs | `docs/revision/_CURRENT_SESSION.md` | Session tracking |
| Docs | `docs/revision/Mangaka/2026-06-21-series-cover-crop-preview-and-strict-card-refresh.md` | This handoff |

## DB/SP impact

**None.** No stored procedures, tables, migrations, or FileResource schema changed.

## Part 1 — Stricter targeted card refresh fallback

### Old behavior (removed)

```text
UpdateDraftAsync succeeds
→ GetMySeriesCardByIdAsync returns null
→ UI fabricates card from command result + local genre/tag selections
```

### New behavior

```text
UpdateDraftAsync succeeds
→ GetMySeriesCardByIdAsync returns valid SeriesDto:
  → Replace card in _seriesData with server-confirmed data
  → Show success snackbar
→ GetMySeriesCardByIdAsync returns null/404:
  → Remove series from _seriesData
  → Show warning: "The draft was saved, but this series is no longer available..."
  → Close edit modal
→ GetMySeriesCardByIdAsync throws exception:
  → Keep existing card unchanged (stale but safer)
  → Show warning: "The draft was saved, but the card could not be refreshed..."
  → Close edit modal
```

The update command result (`SeriesDraftUpdatedDto`) is no longer used to fabricate data. The dashboard only displays server-confirmed refreshed card data after an update.

## Part 2 — Series cover crop preview

### Avatar crop reuse decision

The avatar cropper (`profile-settings-crop.js` + `ProfileSettings.razor`) is:
- Pure JS canvas + Blazor JS interop (no third-party libs)
- Hard-coded to square (1:1) crop with circular border
- Outputs 512x512 PNG
- Tightly integrated into `ProfileSettings.razor`

**Decision: Created a new reusable crop system rather than modifying the avatar cropper.** This avoids risk to existing avatar functionality.

### New JS module (`image-crop.js`)

Based on the avatar cropper pattern but with:
- Configurable `aspectRatio` parameter (default 2/3 for portrait)
- Configurable output dimensions (`outputWidth`, `outputHeight`)
- No circular border (avatar-specific)
- No `window.profileSettingsCrop` global (avatar legacy)
- Independent state map (`imageCropStates` vs `avatarCropStates`)
- Same canvas/pointer-event/drawGrid pattern as avatar cropper

### New component (`ImageCropDialog.razor`)

Reusable Blazor crop dialog component with parameters:

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `SourceDataUrl` | `string` | required | Data URL of image to crop |
| `AspectRatio` | `double` | `2.0/3.0` | Width/height ratio |
| `OutputWidth` | `int` | `1000` | Output image width |
| `OutputHeight` | `int` | `1500` | Output image height (2:3 with 1000) |
| `OutputContentType` | `string` | `"image/png"` | PNG for safety/consistency |
| `CropDescription` | `string` | `"2:3 portrait crop"` | Display label |
| `HelperText` | `string` | Cover helper text | Shown below description |
| `OutputFileNamePrefix` | `string` | `"series-cover"` | Output filename prefix |
| `OnConfirm` | `EventCallback<(byte[], string, string)>` | required | Bytes, filename, content type |
| `OnCancel` | `EventCallback` | required | Cancel callback |

### Create/edit integration

**Create modal:**
- `MudFileUpload` now calls `OnCreateCoverChanged` method
- Method validates file, reads bytes, builds data URL, shows crop dialog
- After crop confirm: stores cropped bytes as `_draftCoverBytes`, shows 80x120 preview
- Title bar shows cropped file name with Clear button (`ClearCreateCover`)

**Edit modal:**
- `OnEditCoverChanged` method now validates file, reads bytes, shows crop dialog
- After crop confirm: stores cropped bytes as `_editCoverBytes`, shows 80x120 preview
- Existing current cover display preserved
- `ClearEditCover` unchanged

### UX changes

- **Create modal:** Cover section now shows 80x120 portrait preview image + filename after crop (was: filename only)
- **Edit modal:** Cover preview now shows cropped 80x120 portrait image (was: raw image preview at 80x113, now consistently 80x120)
- **Crop dialog:** Full-screen overlay with 480x480 canvas, zoom slider, drag-to-reposition, 2:3 crop frame with grid lines
- **Helper text:** "Covers are displayed in a 2:3 portrait frame. Please crop your image to choose the visible cover area."
- **Cancel:** Does not replace current cover selection; user can choose another image

### Output format

- **Content type:** `image/png` (safe, universally supported, matches avatar cropper pattern)
- **File name:** `series-cover-cropped.png`
- **Output size:** 1000 × 1500 px (2:3 portrait)
- **Small image behavior:** Images smaller than 1000×1500 are still accepted; a warning is shown: "This image is smaller than the recommended cover size. It will be resized to 1000×1500, but the final cover may look blurry."

### Backend impact

**None.** The cropped image is uploaded through the existing multipart form flow. No changes to:
- API controller endpoints
- Form contracts (`CreateSeriesDraftForm`, `UpdateSeriesDraftForm`)
- Application commands/handlers
- Cloudinary upload workflow
- SQL stored procedures
- FileResource schema
- `SERIES_COVER` validation rules

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
Build succeeded.
0 errors
57 warnings (all pre-existing baseline, none from changed files)
```

Changed-file warning check:
- `MangakaDashboard.razor` — 0 warnings
- `ImageCropDialog.razor` — 0 warnings
- `App.razor` — 0 warnings
- `image-crop.js` — not compiled

## Manual smoke

Runtime smoke not run; user must verify manually.

### Targeted refresh

```
[ ] Login as Mangaka
[ ] Open /mangaka
[ ] Edit one draft
[ ] Save succeeds
[ ] GetMySeriesCardByIdAsync reloads that card
[ ] Full LoadSeriesAsync is not called after save
[ ] UpdatedAtUtc / "Updated just now" refreshes correctly
[ ] If targeted query returns null/404, UI removes card with warning (does not fabricate fake card)
[ ] If targeted query throws network error, UI keeps existing card with warning
```

### Cover crop

```
[ ] Open Create Draft modal
[ ] Select cover image (PNG, JPG, or WEBP under 5MB)
[ ] Crop dialog opens with 2:3 portrait frame
[ ] Aspect ratio locked to 2:3
[ ] Can drag image to reposition
[ ] Zoom slider works (zoom in/out)
[ ] Reset button resets position and zoom
[ ] Confirm crop
[ ] Cropped 80x120 preview appears in modal
[ ] Create draft — cropped image uploaded as SERIES_COVER
[ ] Open Edit Draft modal
[ ] Current cover shown with "Current" label
[ ] Select replacement cover
[ ] Crop dialog opens
[ ] Confirm crop
[ ] New 80x120 cropped preview appears (replaces current)
[ ] Save edit — cropped image uploaded as replacement SERIES_COVER
[ ] Cancel crop — cover state unchanged, can choose another image
[ ] Backend receives normal image/PNG file
[ ] No DB/schema/SP change required
[ ] Cover displays correctly in ProposalReviewDetail (2:3 frame)
[ ] Cover displays correctly in SeriesPage (220x310)
[ ] Cover displays on Mangaka series cards
```

## Known issues

None from this session.

## Follow-ups

- Runtime smoke testing recommended
- Future: Avatar cropper could be refactored to use unified `ImageCropDialog` component (currently separate `ProfileSettings.razor` integration)
