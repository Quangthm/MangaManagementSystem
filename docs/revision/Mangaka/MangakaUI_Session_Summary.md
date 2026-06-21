# Mangaka UI Overhaul — Full Session Summary

**Branch:** `feature/Mangaka-v2` (created from `feature/Mangaka`)
**Commit:** `eac5c2f`
**Date:** 2026-06-14

---

## Goal

Replace all old Mangaka MudBlazor `.razor` pages with new UI/UX matching the React Figma design in `Manga Creator UI_UX Design`, using **MudBlazor 8.0.0** components (not MUI).

---

## Files Changed (20 files, +1439 / -266)

### New Files

| File | Description |
|------|-------------|
| `Components/Pages/Mangaka/SeriesStatusHelper.cs` | Maps 7 DB status codes to display labels, badge colors, text colors |
| `wwwroot/js/canvas.js` | Canvas JS interop: paper grain, AI badge, cross guides, corner handles, panel drawing |

### Deleted Files

| File | Description |
|------|-------------|
| `Components/Pages/Mangaka/AssignTask.razor` | Legacy page, superseded by dashboard task view |
| `Components/Pages/Mangaka/SeriesList.razor` | Legacy page, superseded by MangakaDashboard |

### Modified Files

| Area | File | Changes |
|------|------|---------|
| **Web** | `MangakaDashboard.razor` | Full rewrite: live DB loading via `GetAllSeriesAsync()`, series card grid with accent bars/genre/status chips, kebab menu (Upload Cover, Favorite, Delete, Change Status submenu, Status badge), New Series Proposal modal (title, synopsis, genre, file upload), Ranking Tracker tab (stats cards, ranking table, poll form), Assistant Review tab (task table with "Open in Workspace" link) |
| **Web** | `CreatorWorkspace.razor` | Canvas tab with JS interop rendering, chapter list with status icons (check/dot/circle) + Active/Draft chips, New Chapter dialog (removed Manuscript Draft, creates empty pages), task panel (hidden by default, toggled via Assignment icon), focused task detail with Cancel/Reassign/Mark Done, Exit button (ArrowBack → `/mangaka`), fixed `_blazorFilesById` null error (pre-read bytes), fixed `ObjectDisposedException` on canvas JS interop |
| **Web** | `MangakaLayout.razor` | Simplified to bare `<main>` wrapper — dark MangaFlow sidebar removed |
| **Web** | `ProposalList.razor` | Minor adjustments for proposal file upload integration |
| **Application** | `ChapterPageTaskService.cs` | Added `GetChapterPageTasksByCreatorUserIdAsync` with full EF Include chain → SeriesId |
| **Application** | `SeriesService.cs` | Added `UpdateSeriesAsync` (returns `SeriesDto?`) |
| **Application** | `SeriesProposalService.cs` | Added `UpdateProposalStatusAsync` + `GetAllProposalsAsync` |
| **Application** | `IChapterPageTaskService.cs` | New interface method `GetChapterPageTasksByCreatorUserIdAsync` |
| **Application** | `ISeriesService.cs` | New interface method `UpdateSeriesAsync` |
| **Application** | `ISeriesProposalService.cs` | New interface methods `UpdateProposalStatusAsync`, `GetAllProposalsAsync` |
| **Application** | `ChapterPageTaskDtos.cs` | Extended `ChapterPageTaskDto` with `TaskTitle`, `TaskDescription`, `SeriesId`, `AssignedToDisplayName` |
| **Application** | `SeriesDtos.cs` | Extended DTOs for update operations |
| **Application** | `SeriesProposalDtos.cs` | Extended DTOs for proposal workflow |
| **Domain** | `PageRegion.cs` | Added `ChapterPageVersion?` navigation property |
| **Domain** | `IChapterPageTaskRepository.cs` | New method `GetByCreatorUserIdWithSeriesAsync` |
| **Infrastructure** | `ChapterPageTaskRepository.cs` | Implements `GetByCreatorUserIdWithSeriesAsync` with `ThenInclude` chain through `PageRegions → ChapterPageVersion → ChapterPage → Chapter` + `AssignedToUser` |
| **Infrastructure** | `PageRegionConfiguration.cs` | Updated EF config for new navigation property |

---

## What Was Done (Detailed)

### 1. MangakaDashboard.razor — Complete Rewrite
- **Layout:** Left sidebar with MangaFlow branding + 4 nav tabs + profile widget popover
- **Series Cards:** Rich grid with accent bar by status, genre/status chips, kebab menu
- **"Add New" card** to open the creation modal
- **Live DB loading** via `GetAllSeriesAsync()` instead of hardcoded demo data
- **Status badge** uses `SeriesStatusHelper` (7 codes: `PROPOSAL_DRAFT`, `UNDER_EDITORIAL_REVIEW`, `UNDER_BOARD_REVIEW`, `SERIALIZED`, `HIATUS`, `CANCELLED`, `COMPLETED`)
- **Kebab menu:**
  - Upload Cover (always visible)
  - Favorite/Unfavorite (in-memory `HashSet<Guid>` toggle)
  - Delete
  - Status badge (only for `SERIALIZED`)
  - "Change Status" submenu calling `UpdateSeriesAsync`

### 2. Cover Upload — End-to-End
- Cloudinary upload via `IFileStorageService` with purpose code `SERIES_COVER`
- `FileResource` DB record created, `CoverFileId` passed to `CreateSeriesAsync`
- Edit Cover modal for `SERIALIZED`/`COMPLETED`/`CANCELLED` series

### 3. New Series Proposal Modal
- Uses `SeriesProposal` fields: ProposalTitle, SynopsisSnapshot, GenreSnapshot
- Proposal document upload (purpose code `SERIES_PROPOSAL`)
- Creates both `Series` + `SeriesProposal` records
- Cover upload removed from create flow (cover added later via Edit Cover)

### 4. CreatorWorkspace Task Panel
- Hidden by default (`_showTaskPanel = false`), toggled via Assignment icon in header
- Center/right grid adjusts responsively (`md=9` / `md=3`)
- Accepts `TaskId` query parameter, loads focused task detail on init
- Action buttons: Cancel (`CANCELLED`), Reassign (inline `MudSelect`), Mark Done (`COMPLETED`)
- Inline `MudSelect` used instead of dialog (MudBlazor 8 removed `MudDialogInstance`)

### 5. Dashboard Tabs
- **Ranking Tracker:** 4 stat cards (Ranking, Score, Total Votes, Weekly Change), ranking table with Risk chips + "Prepare Board Review" action, "Update Weekly Poll Results" form
- **Assistant Review:** Replaced with chapter-page task view — 4 stat cards (Total/Assigned/UnderReview/Completed), full task table with "Open in Workspace" link to `/mangaka/workspace/{seriesId}?taskId={id}`

### 6. New Chapter Dialog
- Removed Manuscript Draft upload section entirely
- After chapter creation, loops from 1 to `_newChapterPages` calling `ChapterPageService.CreateChapterPageAsync()` for each page
- `@onclick:stopPropagation` added to `MudCard` to prevent modal close when clicking form fields

### 7. Bug Fixes
- **`_blazorFilesById` null error:** Pre-read `IBrowserFile` bytes immediately in `FilesChanged` handler, store as `byte[]`, never hold `IBrowserFile` reference across re-renders (fix applied to both proposal upload and edit cover)
- **`ObjectDisposedException` on canvas JS interop:** Added `_disposed` flag set in `DisposeAsync()` before disposing `_canvasModule`; `RenderCanvas()` and `OnAfterRenderAsync()` both check `_disposed` before JS calls
- **Modal click-through:** `@onclick:stopPropagation` on all modal `MudCard` elements

### 8. Service/Repository Layer
- `ChapterPageTaskService` with full Include chain for creator query
- `ChapterPageTaskRepository` with `ThenInclude` through `PageRegions → ChapterPageVersion → ChapterPage → Chapter` + `AssignedToUser`
- Navigation to `SeriesId` from task → series via Chapter → Series relationship
- `UpdateSeriesAsync` on `SeriesService` for status changes

### 9. Layout Simplification
- `MangakaLayout.razor` reduced to `<div class="mf-shell"><main>@ChildContent</main></div>`
- Dark MangaFlow sidebar with links removed (navigation handled per-page)

---

## Key Technical Decisions

| Decision | Rationale |
|----------|-----------|
| Standalone dashboard (no layout wrapper) | Matches React's self-contained sidebar-in-page structure |
| Static `SeriesStatusHelper` class instead of enum | Maps DB string codes directly, no migration needed |
| `UpdateSeriesAsync` returns `SeriesDto?` | Matches existing service pattern (null = not found) |
| Pre-read `IBrowserFile` bytes immediately | Avoids Blazor JS interop `_blazorFilesById` null when upload component removed from DOM during re-render |
| Inline `MudSelect` for reassign instead of dialog | MudBlazor 8 removed `MudDialogInstance`, `@bind-Visible` pattern is incompatible |
| Empty `ChapterPage` records on chapter create | Placeholders for later image upload |

---

## Remaining Work / Next Steps

- Connect chapter page image upload to `ChapterPageService` for persistence
- Add error boundary / fallback UI for unreachable DB
- Wire `FavouriteSeries` to DB persistence (currently in-memory only)
- Replace remaining placeholder tabs with live data
