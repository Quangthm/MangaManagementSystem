# Series UI Follow-up Patch

**Date:** 2026-06-18  
**Branch:** `feature/Mangaka`  
**Base commit:** `3009af8` (Clean up series UI and contributor display)  
**Status:** Uncommitted working-tree changes

---

## Problem

Several UX issues remained after the initial Mangaka dashboard implementation:

1. Card kebab menus and action buttons remained visible on top of open modals (Cancel Draft, New Series Draft, Edit Draft, Submit Proposal, Review Status), causing a confusing overlay with overlapping interactive elements.
2. Contributor panel used a centered `MudDialog` instead of a right-side drawer, inconsistent with the sidebar/dashboard layout paradigm.
3. Dashboard had no pagination — all series rendered on a single scroll, causing performance and UX issues for Mangakas with many series.
4. Search and sort lacked pagination-aware page resets.

## Files Changed

- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor`
- `src/MangaManagementSystem.Web/Components/Pages/Series/SeriesPage.razor`

## Modal/Kebab Fix

**Root cause:** Card kebab menus and action rows were always rendered regardless of modal state, using `z-index: 10` on the kebab wrapper which was insufficient to prevent overlap.

**Fix:**
- Removed `z-index: 10` from the kebab wrapper `<div>` (no longer needed).
- Added `IsAnyDashboardModalOpen` computed property that checks all 5 modal booleans:
  - `_cancelDraftDialogOpen`
  - `_newSeriesDialogOpen`
  - `_draftDetailsOpen`
  - `_reviewStatusOpen`
  - `_submitProposalDialogOpen`
- Wrapped kebab menu with `@if (!IsAnyDashboardModalOpen)` so it is not rendered when any modal is open.
- Wrapped card action rows (View Draft/Submit and View Status buttons) with the same `@if` guard.
- `_openSeriesActionMenuId` is set to `null` in all modal opener methods (already done from prior patch).

## Contributor Drawer

**Before:** Centered `MudDialog` with separate "Active Contributors" and "Past Contributors" sections.

**After:** Right-side `MudDrawer` (`Anchor="End"`, `Variant="DrawerVariant.Temporary"`, 380px width) with:
- Header bar with title and close button
- Filter controls (status + role) in the header area
- Single flat contributor list with `FilteredContributors` computation
- Active/Ended status inline per row
- `IDialogService` injection removed (no longer needed)

### Contributor Filters

**Status filter** (3 options):
- All (default)
- Active (`EndDate == null`)
- Past (`EndDate != null`)

**Role filter** (4 options):
- All roles (default)
- Mangaka
- Tantou Editor
- Assistant

**Behavior:** When `OpenContributorPanel()` is called, both filters reset to "all" before opening the drawer.

## Dashboard Pagination

**Flow:**
```
_seriesData → status filter → search → sort → AllFilteredSeries → paginate → PagedSeriesData → render
```

**Details:**
- Page size: 8 cards
- `_dashboardPage = 1` (default)
- `DashboardTotalPages` computed from `Math.Ceiling((double)AllFilteredSeries.Count / DashboardPageSize)`
- `PagedSeriesData` uses `.Skip().Take()` on `AllFilteredSeries`
- Pagination controls: Prev/Next buttons with numbered page buttons between them
- Pagination controls only shown when `DashboardTotalPages > 1`
- `ResetDashboardPage()` called when filter chip clicked, search text changes (`@bind-Value:after`), or sort mode changes (`@bind-Value:after`)

**Empty state:** Uses `AllFilteredSeries.Count` (not paginated count).

## New Series Draft Entry-point Behavior

Two entry points preserved:
1. **Header button** — primary action, "New Series" button in the top header bar
2. **Grid card** — dashed-border visual shortcut inside the card grid

Both open the same Create Draft modal (`_newSeriesDialogOpen = true`).

The grid New Series Draft card:
- Is rendered **outside** the `@foreach (var series in PagedSeriesData)` loop
- Does not affect pagination page size, total pages, filter counts, search results, or sort order
- Is part of the CSS grid layout but excluded from pagination logic

## Build Result

```
dotnet build MangaManagementSystem.slnx
0 Warning(s)
0 Error(s)
```

All 14 projects built cleanly.

## Manual Test Checklist

- [ ] Open each modal type (Cancel Draft, New Series, Edit Draft, Submit Proposal, Review Status) and verify no kebab menus or action rows are visible behind/over the modal.
- [ ] Verify kebab menu is visible when no modal is open.
- [ ] Open contributor panel — verify right-side drawer slides in with smooth animation.
- [ ] Toggle status filter (All / Active / Past) and verify list updates.
- [ ] Toggle role filter and verify list updates.
- [ ] Verify combined filters work.
- [ ] Verify filter resets on open.
- [ ] Verify contributor rows show: DisplayName, RoleName, Active/Ended, StartDate, EndDate.
- [ ] Navigate pagination — verify Prev/Next buttons and page number buttons work.
- [ ] Verify page resets to 1 when filter chip, search, or sort changes.
- [ ] Verify empty state uses `AllFilteredSeries.Count` not paginated count.
- [ ] Verify New Series Draft card is always visible regardless of pagination state.
- [ ] Verify header "New Series" button opens the same modal as the card.
- [ ] Verify search and sort controls function correctly with pagination.

## Remaining Tasks

- [ ] Wire up `DashboardTotalPages > 1` visibility with `AllFilteredSeries.Count` to show pagination only when needed (done).
- [ ] Finalize any missing `@onclick:stopPropagation` attributes on kebab interactive elements if click-through is observed during manual testing.
