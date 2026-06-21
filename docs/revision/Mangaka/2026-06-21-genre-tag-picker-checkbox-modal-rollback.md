# Genre/Tag Picker Checkbox & Modal Layout Rollback

## Branch

`feature/Mangaka`

## Date

2026-06-21

## Task summary

Targeted rollback of the bad checkbox-based picker UI and broken flexbox modal layout from the previous "modal polish" task. Removed checkboxes entirely from genre/tag picker rows (back to highlighted-row selection). Reverted modal layout to simple single-container scrolling. Kept the timestamp refresh fix (`LoadSeriesAsync` after save edit).

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web page | `Components/Pages/Mangaka/MangakaDashboard.razor` | Removed checkboxes from picker rows, reverted modal layout, kept timestamp reload |

## DB/SP impact

None.

## Changes applied

### 1. Checkbox removal — back to highlighted-row selection

Removed `MudCheckBox` from both genre picker and tag picker rows. Selection is now shown by row background color only:

- Genre: selected rows have `#eef2ff` (indigo tint)
- Tag: selected rows have `#f0fdf4` (green tint)

Selection identity is keyed by actual `GenreId`/`TagId` via `HashSet<Guid>.Contains(id)`. Row click toggles one item. No checkbox, no `CheckedChanged`, no `@onclick:stopPropagation`. No page-index-based visual selection.

### 2. Modal layout — simple single-scroll container

Removed the broken flexbox approach (`display: flex; flex-direction: column`, `flex-shrink: 0`, `overflow-y: auto; flex: 1; min-height: 0`).

**Create modal:** Added `max-height: calc(100vh - 48px); overflow-y: auto` to MudCard. No flexbox. Card scrolls as a single unit. Header and buttons both remain normally sized and reachable.

**Edit modal:** Changed `max-height: 92vh` to `max-height: calc(100vh - 48px)`. Removed `position: sticky; top: 0; background: #fff; z-index: 2` from header (no longer needed since it scrolls with content). No flexbox.

### 3. Timestamp refresh — retained

`SaveDraftEditAsync` calls `await LoadSeriesAsync()` after successful save, reloading the dashboard series list from the API to get the correct `UpdatedAtUtc`. Card shows "Updated just now" immediately without full browser refresh.

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
0 errors, 57 warnings (baseline ~60, down from 61)
```

The MUD0002 warnings from MudCheckBox `CheckedChanged` are gone. Warning count decreased by 4.

## Manual smoke status

Manual smoke not run; build-only verification completed.

### Checklist

```
[ ] Open /mangaka at 100% zoom
[ ] Open Create Draft dialog — header/top reachable, Create button normal height
[ ] Long genre/tag selection does not break modal
[ ] Open Edit Draft dialog — header/top reachable, Save button normal height
[ ] Open Manage Genres — no checkbox boxes displayed
[ ] Selected genres shown by highlighted row only
[ ] Click row toggles exactly that genre by ID
[ ] Page 1 selection does not appear on wrong item on page 2+
[ ] Search/page changes preserve selected IDs
[ ] Cannot unselect final genre
[ ] Open Manage Tags — no checkbox boxes displayed
[ ] Selected tags shown by highlighted row only
[ ] Click row toggles exactly that tag by ID
[ ] Page/search changes preserve selected IDs
[ ] Save Edit updates "Updated X mins ago" immediately
```

## Known issues

- Handler DTO returns empty Genres/Tags (known from prior handoff)
- Handler DTO missing UpdatedAtUtc (workaround: LoadSeriesAsync reload)

## Follow-ups

- Fix handler DTO to return populated Genres/Tags and UpdatedAtUtc
