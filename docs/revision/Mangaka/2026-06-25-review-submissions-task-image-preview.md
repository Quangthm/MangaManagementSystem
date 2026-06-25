# Review Submissions Task Image Preview (Revised)

## Branch
feature/Mangaka

## Date
2026-06-25

## Summary
Fixed oversized task-card page images and redesigned the image preview as a true fullscreen lightbox. Moved all styling out of global `mangaflow.css` into component-scoped CSS.

### Revision
The first implementation had three issues discovered during runtime smoke:
1. Thumbnail wrapper lacked strict `max-width` / `min-width` enforcement — wide images could expand the task card
2. Preview used a `MudPaper` card embedded inline rather than a fullscreen viewport-centered lightbox — it felt like a box over the same image
3. `.task-page-thumb` CSS class was in global `wwwroot/css/mangaflow.css` — could affect shared/teammate views

## Scope
- Thumbnail: 130×190px portrait bounding box with `max-width: 130px; min-width: 130px; max-height: 190px` — strictly enforced regardless of source image dimensions
- Preview: Fullscreen `MudOverlay` with `DarkBackground="true"`, viewport-centered `<img>`, close button fixed at top-right
- CSS: All styles moved to `ImagePreview.razor.css` (Blazor CSS isolation)
- Dual-page manga spread display: intentionally scoped to thumbnail bounding only — splitting/cropping deferred to workspace page-processing task

## Files changed
| File | Change |
|------|--------|
| `Components/Shared/ImagePreview.razor` | Rewritten: `<button>` thumbnail with strict bounds, fullscreen lightbox preview, simplified `[Parameter]` API |
| `Components/Shared/ImagePreview.razor.css` | **New** — component-scoped CSS with `.mf-ip-thumb`, `.mf-ip-overlay`, `.mf-ip-lightbox`, `.mf-ip-full`, `.mf-ip-close`, `.mf-ip-title` |
| `Components/Pages/Mangaka/ReviewSubmissions.razor` | Removed `ThumbnailCssClass="task-page-thumb"` — component owns its styling now |
| `wwwroot/css/mangaflow.css` | Removed `.task-page-thumb` and `.task-page-thumb img` |
| `docs/revision/Mangaka/2026-06-25-review-submissions-task-image-preview.md` | This file (updated) |

## Removed from global CSS
```css
.task-page-thumb { width:130px; height:190px; max-width:100%; ... }
.task-page-thumb img { width:100%; height:100%; ... }
```
These were in `wwwroot/css/mangaflow.css` (lines 3030-3052). Now removed.

## Component-scoped CSS classes (ImagePreview.razor.css)
| Class | Purpose |
|-------|---------|
| `.mf-ip-thumb` | Thumbnail button — 130×190px, strict bounds, cursor:zoom-in |
| `.mf-ip-thumb-img` | Image inside thumbnail — contained to parent bounds |
| `.mf-ip-overlay` | Fullscreen overlay — `position:fixed; inset:0; z-index:2000` |
| `.mf-ip-lightbox` | Centering wrapper — `100vw × 100vh` flex centering |
| `.mf-ip-full` | Full preview image — `max-width: min(94vw, 1200px); max-height: 88vh` |
| `.mf-ip-close` | Close button — fixed top-right, circular, semi-transparent |
| `.mf-ip-title` | Title bar — fixed bottom-center |

## Thumbnail sizing
- Enforced portrait bounds: `width: 130px`, `max-width: 130px`, `min-width: 130px`, `height: 190px`, `max-height: 190px`
- Wide/double-page images: contained via `object-fit: contain` — cannot expand beyond the 130×190 box
- Task card: remains compact at 100% zoom

## Lightbox/preview behavior
- Dark fullscreen backdrop over viewport (`DarkBackground="true"`)
- Image centered in viewport, not relative to task card
- Large image constrained: `max-width: min(94vw, 1200px); max-height: 88vh`
- Close button: fixed × button at top-right of screen
- Outside click closes (MudOverlay `AutoClose="true"`)
- `@onclick:stopPropagation` prevents interaction with underlying task card

## Component API (simplified)
```razor
<ImagePreview ImageUrl="@url"
              AltText="Original page"
              Title="Original Page Preview" />
```
No CSS class parameters needed externally — component owns its styling.

## Build result
All compilation succeeded. File-copy errors (MSB3027) are from the running app holding file locks — normal with hot-reload. No new compilation warnings or errors from these changes.

## Runtime smoke
Pending — run and verify:
- [ ] Thumbnail is exactly 130×190px regardless of source image size
- [ ] Wide/double-page image stays contained inside thumbnail
- [ ] Task card does not expand
- [ ] Clicking thumbnail opens fullscreen dark overlay
- [ ] Preview image is centered in viewport
- [ ] Close button works
- [ ] Outside click closes
- [ ] Task buttons (Approve, Return, Cancel, Reassign, View in Workspace) still work
- [ ] Quick Select still works

## Existing behavior preserved
- Quick Select dialog and task creation
- Approve / Return for Rework / Cancel / Reassign
- Workspace links
- Task filters
- RefreshTasksAfterMutationAsync()

## Follow-up
- **Workspace page-processing task**: Detect/guide users when uploading double-page manga spreads; optionally split/crop into single page images before page version creation. This is separate from the display fix in this task.
- **Global reuse**: ImagePreview component can be dropped into any page needing clickable image preview (series covers, proposals, workspace pages).

## Next step
Runtime smoke test with a real double-page manga image to verify thumbnail bounds hold and lightbox works as fullscreen.
