# Review Submissions Task Image Preview (Revised)

## Branch
feature/Mangaka

## Date
2026-06-25

## Summary
Fixed oversized task-card page images and redesigned the image preview as a true viewport-level lightbox. The earlier CSS-isolated `MudOverlay` attempt failed runtime smoke because the thumbnail was not reliably constrained and the preview stayed visually trapped in the task-card layout. Final implementation uses `MudDialog` via the app-level dialog provider.

### Revision
The first implementation had three issues discovered during runtime smoke:
1. Thumbnail wrapper relied too heavily on isolated CSS — wide images could still expand the task card when CSS isolation didn't apply at runtime
2. Preview used an inline overlay inside the component tree rather than an app-level dialog — it felt attached to the same card area
3. Global/shared styling risk was avoided, but the display behavior still failed because the overlay approach was not viewport-scoped enough

## Scope
- Thumbnail: 130×190px portrait bounding box with hard inline bounds and `object-fit: contain` — strictly enforced regardless of source image dimensions
- Preview: Fullscreen `MudDialog` with dark fullscreen content, viewport-centered `<img>`, close button fixed at top-right
- CSS: Component-scoped CSS remains minimal; critical layout styles are inline fallback to avoid CSS isolation/runtime issues
- Dual-page manga spread display: intentionally scoped to thumbnail bounding only — splitting/cropping deferred to workspace page-processing task

## Files changed
| File | Change |
|------|--------|
| `Components/Shared/ImagePreview.razor` | Rewritten: fixed-size thumbnail button, `IDialogService` preview launch, simplified `[Parameter]` API |
| `Components/Shared/ImagePreviewDialog.razor` | **New** — fullscreen lightbox dialog rendered through `MudDialogProvider` |
| `Components/Shared/ImagePreview.razor.css` | Reduced to non-critical enhancement styles only |
| `Components/Shared/ImagePreviewDialog.razor.css` | **New** — intentionally minimal; critical layout is inline fallback |
| `Components/Pages/Mangaka/ReviewSubmissions.razor` | Continues to use `<ImagePreview ... />`; no full-width wrapper added |
| `docs/revision/Mangaka/2026-06-25-review-submissions-task-image-preview.md` | This file (updated) |

## Thumbnail sizing
- Enforced portrait bounds: `width: 130px`, `max-width: 130px`, `min-width: 130px`, `height: 190px`, `max-height: 190px`
- Wide/double-page images: contained via `object-fit: contain` — cannot expand beyond the 130×190 box
- Task card: remains compact at 100% zoom

## Lightbox/preview behavior
- Dark fullscreen dialog content over viewport
- Image centered in viewport, not relative to task card
- Large image constrained: `max-width: min(94vw, 1200px); max-height: 88vh`
- Close button: fixed × button at top-right of screen
- Backdrop/escape close supported by MudBlazor dialog options
- `@onclick:stopPropagation` prevents interaction with underlying task card

## Component API (simplified)
```razor
<ImagePreview ImageUrl="@url"
              AltText="Original page"
              Title="Original Page Preview" />
```
No CSS class parameters needed externally — component owns its styling.

## Build result
`dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental` succeeded with `0 errors` and `43 warnings` (same baseline warning count).

## Runtime smoke
Pending manual browser smoke at 100% zoom on `/mangaka/review-submissions`:
- [ ] Thumbnail is exactly 130×190px regardless of source image size
- [ ] Wide/double-page image stays contained inside thumbnail
- [ ] Task card does not expand
- [ ] Clicking thumbnail opens fullscreen dark dialog
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
Runtime smoke test with a real double-page manga image to verify thumbnail bounds hold and lightbox works as a true viewport-level dialog.
