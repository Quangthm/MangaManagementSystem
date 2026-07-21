# Genre/Tag UI — Simplified Expandable Chip List

**Date:** 2026-07-20
**Branch:** `feature/Mangaka`
**Status:** Implemented; build verified

## Goal

Simplify the Genres and Tags metadata UI on the `/series/{slug}` page. Remove previous/next pagination and page-number indicators in favor of a simple "preview first 6" / "Show all (N)" / "Show less" pattern.

## What changed

### New file
- `ExpandableChipList.razor` — replaces `ExpandablePaginatedChipList.razor`

### Deleted file
- `ExpandablePaginatedChipList.razor` — old paginated version

### Modified file
- `SeriesPage.razor` — updated component references to new name

## Component design

- **Parameters:** `Title`, `Items` (IReadOnlyList\<string\>), `PreviewCount` (int, default 6)
- **Internal state:** `_expanded` (bool) — per-instance, independent
- **Collapsed:** shows first N items (N = EffectivePreviewCount); shows "Show all (N)" when items > N
- **Expanded:** shows all items; shows "Show less"
- **Empty/≤N:** no controls rendered
- **Removed:** `_currentPage`, page calculation, Prev/Next arrows, page indicator, `OnParametersSet` page clamping, `PageSize` parameter
- Chip styling preserved exactly: `background:#f1f5f9; color:#475569; font-size:0.72rem; padding:3px 10px; border-radius:12px; font-weight:500` with gentle overflow containment

## Backend impact

None. No SeriesDetailDto, handler, repository, API, or database changes.

## Build

```
0 Error(s)
68 Warning(s) — all pre-existing
git diff --check → clean
```

## Files

| File | Status |
|------|--------|
| `Components/Pages/Series/ExpandableChipList.razor` | New |
| `Components/Pages/Series/ExpandablePaginatedChipList.razor` | Deleted |
| `Components/Pages/Series/SeriesPage.razor` | Modified (component name only) |
