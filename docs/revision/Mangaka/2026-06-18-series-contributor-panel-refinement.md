# Series Contributor Panel Refinement

**Branch:** `feature/Mangaka`  
**Date:** 2026-06-18  
**Commit:** 72f0e8c (working tree has unstaged modifications after build)

## Problem

The series slug page (`/series/{slug}`) used a right-side `MudDrawer Anchor="End"` for the contributor panel. The desired UX moved this to an inline expandable panel on the right side of the series header card, without a slide-out drawer, close button, or centered popup.

## Files Changed

Only one file was modified:

- `src/MangaManagementSystem.Web/Components/Pages/Series/SeriesPage.razor`

## MudDrawer Removed

**Yes.** The entire `<MudDrawer>` block (with `MudDrawerHeader`, close button, filter selects, and contributor rows) was deleted. The `@* ── Contributor panel drawer (right side) ── *@` comment block was also removed.

## Contributor Sidebar (Outside Series Card)

The contributor panel is a **separate sidebar card outside the series detail card**, positioned via a page-level CSS grid.

### Page-level grid layout

A `.series-page-grid` CSS class wraps the main content area when contributors exist:

```css
.series-page-grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 320px;
    gap: 24px;
    align-items: start;
}
@media (max-width: 800px) {
    .series-page-grid {
        grid-template-columns: 1fr;
    }
}
```

Grid placement:
- **Left column** (implicit): series detail card (cover + metadata) + chapter list
- **Right column** (`grid-column: 2; grid-row: 1 / span 2`): contributor sidebar card

The chapter list explicitly sets `grid-column: 1` to stay in the left column.

### Series detail card layout (inside left column)

The series card uses a two-column flex layout (no third contributor column):

```
[Cover (220px) | Main info (flex: 1)]
```

### Contributor sidebar behavior

- The trigger "View all contributors" with a chevron icon (`ExpandMore`/`ExpandLess`) sits at the top of the sidebar card.
- **Clicking the trigger toggles open/close.** No X close button. No MudDrawer overlay.
- When open, the panel shows filter controls and contributor rows with `max-height: 340px; overflow-y: auto`.
- The chevron flips: `ExpandMore` (▾) when closed, `ExpandLess` (▴) when open.
- If `_series.Contributors.Count == 0`, the entire sidebar card is hidden (no empty grid gap; the class becomes `null` and the wrapper acts as a plain block div).

### Responsive behavior

At `max-width: 800px`, the grid collapses to a single-column layout, stacking the series card, contributor sidebar, and chapter list vertically.

The outer container has `max-width: 1160px` (previously 900px) to accommodate the wider two-column layout.

## Filter Behavior

Preserved unchanged:

- `_contributorFilterStatus` (all / active / past)
- `_contributorFilterRole` (all / Mangaka / Tantou Editor / Assistant)
- `FilteredContributors` computed property (same LINQ logic)
- **On open**: filters reset to "all" for both status and role
- **On close**: filters stay as-is (state preserved between toggles)

Contributor rows show: DisplayName, RoleName, Active/Ended badge, StartDate, EndDate (if ended).

## State Changes

| Removed | Added |
|---------|-------|
| `_contributorDrawerOpen` (bool) | `_contributorPanelOpen` (bool) |
| `OpenContributorPanel()` method | `ToggleContributorPanel()` method |

`ToggleContributorPanel()`:
```csharp
private void ToggleContributorPanel()
{
    if (!_contributorPanelOpen)
    {
        _contributorFilterStatus = "all";
        _contributorFilterRole = "all";
    }
    _contributorPanelOpen = !_contributorPanelOpen;
}
```

## Responsive Layout Note

The page uses a CSS grid layout (`.series-page-grid`) with a responsive breakpoint at `max-width: 800px` that collapses the two-column grid to a single-column layout. The outer container is `max-width: 1160px`.

## Build Result

```
Build succeeded.
    58 Warning(s)     <-- all pre-existing, none from SeriesPage.razor
    0 Error(s)
```

## Manual Test Checklist

1. [ ] Navigate to `/series/{slug}` for a series with contributors.
2. [ ] Verify the page shows a two-column grid layout (left: series card + chapter list, right: contributor sidebar).
3. [ ] Verify the series card shows two-column layout: cover (220px) + main info (flex: 1) — no third column.
4. [ ] Verify "View all contributors" with `▾` shows in the right sidebar card.
5. [ ] Click "View all contributors" → panel opens with `▴`, filters reset to "All".
6. [ ] Change filters → narrow contributor list.
7. [ ] Close panel → click again → filters are reset to "All" again.
8. [ ] Toggle filters without closing → filters remain applied.
9. [ ] Verify contributor rows show: DisplayName, RoleName, Active/Ended, dates.
10. [ ] Scroll test: many contributors → panel scrolls within `max-height: 340px`.
11. [ ] Series with 0 contributors → right sidebar card hidden entirely; page uses block layout.
12. [ ] Resize browser below 800px → layout collapses to single column.
13. [ ] No MudDrawer overlay slides in from the right.
14. [ ] No X close button visible.
15. [ ] "Open Workspace" button and other UI elements function normally.

## Remaining Tasks

- (none identified — feature complete per requirements)
