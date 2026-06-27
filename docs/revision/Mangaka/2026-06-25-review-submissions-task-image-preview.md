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

### Follow-up revision
- Lightbox worked after dialog implementation.
- Thumbnail was too small at 130x190.
- Final task-card thumbnail size changed to 190x285.
- Thumbnail uses `cover` with centered positioning for task-card display.
- Fullscreen preview still uses `contain` to show the full image.

### Follow-up revision: task-card layout balance
- Lightbox and thumbnail sizing worked, but runtime smoke showed task cards had excessive empty middle/right space.
- Root cause: task metadata and image were stacked on the left while each card occupied a wide 2-column grid cell.
- Fix: added internal grid layout with metadata-left / media-right / footer-actions.
- However, this did NOT visually apply because CSS isolation was not loading.

### Follow-up revision: actual rendered card layout fix and pagination

- Previous layout-balance CSS did not visibly affect runtime.
- Root cause: `App.razor` was missing `<link href="MangaManagementSystem.Web.styles.css" rel="stylesheet" />`. The Blazor CSS isolation bundle was never served. All `.razor.css` files (including `ImagePreview.razor.css` and `ReviewSubmissions.razor.css`) had zero runtime effect.
- Fix: added the scoped CSS bundle link to `App.razor` AND added inline style fallbacks on all critical layout containers (grid, media column, footer) so the layout works even if CSS isolation fails.
- Layout: each task card now uses CSS grid with `grid-template-columns: minmax(0,1fr) auto` — metadata on left, image thumbnail(s) on right, footer actions spanning full width below.
- Pagination: added client-side pagination over the filtered task list. Default page size 6 (3 rows of 2 cards). All filter/search changes reset to page 1. `RefreshTasksAfterMutationAsync` clamps page number.
- Runtime smoke at 100% zoom: pending manual verification.

## Scope
- Thumbnail: 190x285 portrait bounding box with hard inline bounds and `object-fit: cover` for task-card identification
- Preview: Fullscreen `MudDialog` with dark fullscreen content, viewport-centered `<img>`, close button fixed at top-right
- CSS: Critical layout styles are inline on task-card containers. Page-scoped CSS provides additional refinement. Scoped CSS bundle link now added to `App.razor`.
- Pagination: client-side pagination with `MudPagination`, page size 6, filter-reset behavior
- Dual-page manga spread display: intentionally scoped to thumbnail display only — splitting/cropping deferred to workspace page-processing task

## Files changed
| File | Change |
|------|--------|
| `Components/App.razor` | Added `MangaManagementSystem.Web.styles.css` link — enables Blazor CSS isolation for entire app |
| `Components/Shared/ImagePreview.razor` | Added reusable thumbnail parameters (`ThumbnailWidth`, `ThumbnailHeight`, `ThumbnailObjectFit`) with hard inline bounds |
| `Components/Shared/ImagePreviewDialog.razor` | Fullscreen lightbox dialog rendered through `MudDialogProvider`; full preview uses `object-fit: contain` |
| `Components/Shared/ImagePreview.razor.css` | Non-critical enhancement styles (hover, focus ring) |
| `Components/Pages/Mangaka/ReviewSubmissions.razor` | Internal card grid layout with inline style fallbacks; client-side pagination with `MudPagination`; filter/search reset page to 1 |
| `Components/Pages/Mangaka/ReviewSubmissions.razor.css` | Page-scoped CSS for card grid and responsive stacking (now loads via scoped CSS bundle) |
| `docs/revision/Mangaka/2026-06-25-review-submissions-task-image-preview.md` | This file (updated) |

## Thumbnail sizing
- Enforced task-card thumbnail bounds: `width: 190px`, `max-width: 190px`, `min-width: 190px`, `height: 285px`, `max-height: 285px`
- Task-card thumbnail uses `object-fit: cover; object-position: center`
- Wide/double-page images are intentionally cropped in the task card so they stay recognizable instead of shrinking into a thin strip
- Thumbnail is displayed in a dedicated right-side media column so the card width is used more efficiently

## Lightbox/preview behavior
- Dark fullscreen dialog content over viewport
- Image centered in viewport, not relative to task card
- Large image constrained: `max-width: min(94vw, 1200px); max-height: 88vh`
- Full preview still uses `object-fit: contain`
- Close button: fixed x button at top-right of screen
- Backdrop/escape close supported by MudBlazor dialog options
- `@onclick:stopPropagation` prevents interaction with underlying task card

## Pagination
- Client-side pagination over `_filteredTasks`
- Default page size: 6 tasks per page (3 rows of 2 cards)
- All filter changes (status, type, assistant, search text) reset page to 1
- `RefreshTasksAfterMutationAsync` clamps page number to valid range
- `MudPagination` component shown below cards when total pages > 1
- Count summary shown above cards: "Showing X-Y of N tasks"

## Component API
```razor
<ImagePreview ImageUrl="@url"
              AltText="Original page"
              Title="Original Page Preview"
              ThumbnailWidth="190"
              ThumbnailHeight="285"
              ThumbnailObjectFit="cover" />
```

## Build result
`dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental` succeeded with `0 errors` and `43 warnings` (same baseline).

## Runtime smoke
Pending manual browser smoke at 100% zoom on `/mangaka/review-submissions`:
- [ ] Task metadata is on the left side of each card
- [ ] Original Page image is on the right side of each card
- [ ] Footer buttons span below the card body
- [ ] The huge blank middle/right area is visibly reduced
- [ ] Two-card-per-row layout looks acceptable
- [ ] Thumbnail remains medium-sized and recognizable
- [ ] Lightbox still opens correctly
- [ ] Pagination shows when task count exceeds 6
- [ ] Only current page tasks render
- [ ] Showing count is correct
- [ ] Next/previous/page number works
- [ ] Filters reset to page 1
- [ ] Empty state still works
- [ ] Task buttons still work
- [ ] View in Workspace still works
- [ ] Quick Select still works

## Existing behavior preserved
- Quick Select dialog and task creation
- Approve / Return for Rework / Cancel / Reassign
- Workspace links
- Task filters
- RefreshTasksAfterMutationAsync()

## Follow-up
- **Workspace page-processing task**: Detect/guide users when uploading double-page manga spreads; optionally split/crop into single page images before page version creation. This is separate from the display fix in this task.
- **Global reuse**: `ImagePreview` can now be reused elsewhere with caller-provided thumbnail sizing.
- Double-page spread splitting/cropping belongs to future workspace/page-processing task.
