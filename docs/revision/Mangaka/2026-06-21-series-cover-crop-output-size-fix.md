# Series Cover Crop Output Size Fix (1000x1500)

## Branch

`feature/Mangaka`

## Date

2026-06-21

## Task summary

Adjusted series cover crop output from 800×1200 to the agreed project standard of 1000×1500 PNG. Added upscale warning for source images smaller than the target output size.

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web | `Components/Shared/ImageCropDialog.razor` | Changed default `OutputWidth` 800→1000, `OutputHeight` 1200→1500; added `getImageDimensions` JS interop call; added upscale warning UI and state fields |
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Updated `ImageCropDialog` usage: `OutputWidth="1000"`, `OutputHeight="1500"` |
| Web | `wwwroot/js/image-crop.js` | Added `getImageDimensions(canvasId)` export returning `{ naturalWidth, naturalHeight, cropSourceWidth, cropSourceHeight }` |
| Docs | `docs/revision/Mangaka/2026-06-21-series-cover-crop-preview-and-strict-card-refresh.md` | Updated output size defaults in parameter table and output format section |

## DB/SP impact

None.

## Backend impact

None. Backend receives the cropped 1000×1500 PNG through the unchanged multipart form upload flow.

## Output size change

| Aspect | Before | After |
|--------|--------|-------|
| Output width | 800 px | **1000 px** |
| Output height | 1200 px | **1500 px** |
| Aspect ratio | 2:3 (unchanged) | 2:3 (unchanged) |
| Content type | image/png (unchanged) | image/png (unchanged) |
| File name | series-cover-cropped.png | series-cover-cropped.png |

## Small-image upscale behavior

- Source images smaller than 1000×1500 are still accepted
- Canvas upscales the crop area to 1000×1500
- A warning banner is shown in the crop dialog controls panel:

  > This image is smaller than the recommended cover size. It will be resized to 1000×1500, but the final cover may look blurry.

- Detection: natural image dimensions are read from the JS module via `getImageDimensions` after `initialize`. Warning shown when `naturalWidth < 1000` or `naturalHeight < 1500`.

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
Build succeeded.
0 errors
57 warnings (all pre-existing baseline, none from changed files)
```

## Manual smoke

Runtime smoke not run; user must verify manually.

```
[ ] Create cover crop outputs 1000×1500
[ ] Edit cover crop outputs 1000×1500
[ ] Smaller source image can still be cropped and outputs 1000×1500
[ ] Smaller source image shows blur/upscale warning
[ ] Uploaded multipart file is the cropped PNG, not the original image
[ ] Filename: series-cover-cropped.png
[ ] Content type: image/png
[ ] Backend/API/MediatR/Cloudinary/FileResource/SP/DB unchanged
[ ] Build passes with 0 errors
```
