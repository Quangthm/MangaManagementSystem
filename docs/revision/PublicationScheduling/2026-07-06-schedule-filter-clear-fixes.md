# Schedule Filter Clear & State Sync Fixes

**Date:** 2026-07-06
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors, 66 pre-existing warnings, none from changed files)

---

## Task summary

Fixed 4 UI bugs in the Publication Schedule page and Manage Schedule drawer related to series filter display, state synchronization, and chapter search clearing. Web-only fix — no API, Application, Infrastructure, or database changes.

---

## Architecture path

```
Web only — Razor component state synchronization fix.
ScheduleCalendar (owner)
  → PublicationScheduleActionDrawer (consumer, requests clear via EventCallback)
```

No backend changes.

---

## Files changed

| Layer | File | Change |
|---|---|---|
| Web | `Components/Pages/Publication/ScheduleCalendar.razor` | Added `OnClearSeriesRequested="ClearSeriesFilterAsync"` attribute to drawer tag |
| Web | `Components/Pages/Publication/PublicationScheduleActionDrawer.razor` | Refactored: added computed properties, separated load methods, fixed 4 filter bugs |

---

## Root causes fixed

### Bug 1 — Initial series filter stuck in "Loading..."
**Cause:** `_selectedSuggestion` never populated from URL `seriesId`. `SelectedSeriesTitle` parameter always null → chip showed "Loading..." forever.
**Fix:** `ResolveSeriesDisplayTitle()` extracts series title from loaded chapter data after `LoadChaptersAsync` completes. Fallback chain: `SelectedSeriesTitle ?? _resolvedSeriesTitle ?? "Selected series"`.

### Bug 2 — Clearing and reselecting same series does not apply filtering
**Cause:** `ClearSeriesFilter()` (now `ClearSelectedSeriesAsync`) did not reset `_lastLoadedSeriesId`. Re-selecting same series made `currentSeriesId == _lastLoadedSeriesId` → `OnParametersSetAsync` skipped reload.
**Fix:** `ClearSelectedSeriesAsync()` resets `_lastLoadedSeriesId = null`, `_selectedSeriesId = null`, `_resolvedSeriesTitle = null`, and invokes parent `OnClearSeriesRequested` callback. Parent clears → `SelectedSeriesId` propagates as null → re-selection triggers full reload path.

### Bug 3 — Editor clear series filter does not work like Mangaka
**Cause:** `ClearSeriesFilter()` called `ApplyFilters()` client-side only. Editor's `_allChapters` was pre-filtered by API `seriesId` — clearing client-side had no visible effect.
**Fix:** `ClearSelectedSeriesAsync()` invokes parent `OnClearSeriesRequested` → parent `ClearSeriesFilterAsync` → `_selectedSeriesId = null` → `LoadSchedule()` → `StateHasChanged()` → drawer `OnParametersSetAsync` detects `SelectedSeriesId = null` → Editor calls `LoadChaptersAsync()` without seriesId → full unfiltered reload.

### Bug 4 — Clearing chapter search does not unfilter chapter list
**Cause:** Chapter `MudAutocomplete` had `ResetValueOnEmptyText="false"`. Clicking X did not propagate null through `ValueChanged` → `_selectedChapter` never cleared.
**Fix:** Changed to `ResetValueOnEmptyText="true"`. Clicking X now propagates null → `OnChapterFilterChanged(null)` → `ApplyChapterFilters()` unfilters by chapter.

---

## Maintainability improvements

| Before | After |
|---|---|
| Inline `_isEditor` branching in `LoadChaptersAsync` (30 lines in one method) | `LoadChaptersAsync` dispatches to `LoadMangakaChaptersAsync()` / `LoadEditorChaptersAsync()` |
| `_selectedSeriesId.HasValue` inline in 4 places | `HasSeriesFilter` computed property |
| `_selectedChapter != null` inline in 1 place | `HasChapterFilter` computed property |
| `SelectedSeriesTitle ?? "Loading..."` hardcoded in markup | `SeriesChipLabel` with proper 3-level fallback |
| `ClearSeriesFilter()` — one method doing broken partial clear | `ClearSelectedSeriesAsync()` + `ClearChapterSearch()` — each single responsibility |
| `ApplyFilters` scattered across 5 call sites | `ApplyChapterFilters` — consistent name |

Correction: `ApplyFilters` was already consistently named within the file (5 call sites). The rename to `ApplyChapterFilters` is a clarity improvement.

---

## UI behavior changed

- **Series chip** now shows real series title on page load (resolved from loaded chapters), never stuck on "Loading..."
- **Series filter clear** (both via MudChip X in drawer and autocomplete X in calendar) now correctly unfilters for both Mangaka and Editor
- **Re-selecting same series** after clear now re-applies filtering correctly
- **Chapter search clear** now restores full chapter list
- **Editor clear** reloads from API without seriesId, showing all actionable chapters

---

## Verification

### Build

```
dotnet build .\MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
Result: **SUCCESS** — 0 errors, 66 warnings (all pre-existing, none from changed files)

### Manual smoke

Not run (build-only verification).

---

## Known issues

1. **`_resolvedSeriesTitle` may briefly show wrong series title** if the first chapter in `_allChapters` belongs to a different series than `_selectedSeriesId`. Mitigated by the guard `_allChapters[0].SeriesId == _selectedSeriesId.Value` in `ResolveSeriesDisplayTitle()`.
2. **URL not updated on series filter change from drawer** — only parent autocomplete updates the URL. Acceptable MVP behavior.

---

## Follow-ups

None.

---

## Docs/Revision Handoff Path

`docs/revision/PublicationScheduling/2026-07-06-schedule-filter-clear-fixes.md`
