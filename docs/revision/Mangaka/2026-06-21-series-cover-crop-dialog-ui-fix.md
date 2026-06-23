# Series Cover Crop Dialog UI Fix

## Branch

`feature/Mangaka`

## Date

2026-06-21

## Task summary

Fixed the series cover crop dialog UI that was rendering as unstyled HTML at the bottom-left of the page. Rewrote `ImageCropDialog.razor` using MudBlazor components and improved the crop canvas visibility.

## Root cause of bad UI

The original `ImageCropDialog.razor` used CSS classes (`mf-crop-overlay`, `mf-crop-dialog`, `mf-crop-header`, etc.) whose styles are defined in `ProfileSettings.razor`'s `<style>` block. In Blazor Web App with `InteractiveServer` render mode, those styles are only loaded when `ProfileSettings.razor` is on the active page. Since `MangakaDashboard.razor` uses `ImageCropDialog` without `ProfileSettings` on the page, none of the `mf-crop-*` styles were applied, causing the dialog to render as raw unstyled HTML.

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web | `Components/Shared/ImageCropDialog.razor` | Complete rewrite: raw HTML/CSS classes → MudBlazor components (MudOverlay, MudCard, MudText, MudSlider, MudButton, MudAlert, MudIconButton) |
| Web | `wwwroot/js/image-crop.js` | Reduced crop padding from 70px to 40px for better canvas width fit for 2:3 portrait |

## Dialog layout fix summary

### Before (broken)
- Raw `<div class="mf-crop-overlay">` — styles not loaded on MangakaDashboard page
- Styled as page content (appeared at bottom-left)
- Raw `<button>`, `<input type="range">`, `<h2>`, `<p>` — unstyled
- Canvas: 480×480px, 70px padding → crop area only ~273×410px in a square
- All styling depended on ProfileSettings.razor global CSS

### After (fixed)
- **MudOverlay** with `DarkBackground="true"` — proper fullscreen backdrop, z-index stacking
- **Centered wrapper** (`display: flex; align-items: center; justify-content: center`) — dialog appears centered in viewport
- **MudCard** (960px max, 94vw, 90vh max-height, scrollable body) — consistent with project's other modals
- **MudText** for title (Typo.h6) and helper text (Typo.body2, Typo.caption) — styled typography
- **MudIconButton** for close — themed close button
- **MudSlider** (Min=1, Max=3, Step=0.01, Immediate=true) — styled zoom slider with live feedback
- **MudButton** (Variant.Outlined / Variant.Filled, Color.Primary) — styled action buttons
- **MudAlert** (Severity.Warning, Dense) — styled upscale warning banner
- **Canvas** 640×640px with inline styles — no external CSS dependency
- Only a tiny `<style>` block for the `is-dragging` cursor class (used by JS)
- Two-column grid layout: canvas left (flexible width), controls right (270px)

## Crop fit/width behavior summary

- **JS padding:** 70px → **40px**
- **Canvas:** 480×480 → **640×640**
- **Crop area at 2:3:** ~273×410 → **~400×600**
- The crop frame now fills much more of the canvas, giving better horizontal visibility for 2:3 source images
- Minimum zoom still ensures the image completely fills the crop frame
- Output dimensions unchanged: 1000×1500

## Output size/ratio confirmation

- Output: **1000×1500 PNG** (unchanged)
- Aspect ratio: **2:3** (unchanged)
- Content type: **image/png** (unchanged)
- Filename: **series-cover-cropped.png** (unchanged)
- Backend receives cropped file only (unchanged)

## Backend impact

None. No changes to API, MediatR, Cloudinary, FileResource, stored procedures, or database schema.

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
Build succeeded.
0 errors
57 warnings (all pre-existing baseline, none from changed files)
```

Changed-file warning check: **0 new warnings**.

## Manual smoke

Runtime smoke not run; user must verify manually.

```
[ ] Create Draft → select cover → crop dialog appears as polished MudBlazor modal
[ ] Dialog is centered in the viewport
[ ] No raw unstyled text/buttons
[ ] Crop canvas is correctly positioned (not bottom-left)
[ ] Zoom slider (MudSlider) works with live feedback
[ ] Zoom +/- buttons work (+/- 0.1 step)
[ ] Reset button resets position and zoom
[ ] Small-image warning uses MudAlert (styled yellow banner)
[ ] Use This Image closes dialog and shows preview
[ ] Cancel closes dialog without replacing cover
[ ] Edit Draft cover replacement works the same
[ ] Output remains 1000×1500 PNG
[ ] Canvas is now 640×640 with 40px padding (wider crop frame)
[ ] Backend still receives cropped file only
```
