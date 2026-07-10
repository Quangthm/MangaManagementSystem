# Schedule Slug URL + Filter State Fixes

**Date:** 2026-07-06
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors, 66 pre-existing warnings, none from changed files)

---

## Task summary

1. Changed Publication Schedule URL identity from internal `seriesId` to user-facing `series={seriesSlug}`
2. Fixed 4 remaining UI bugs in series filter state sync, chapter search scoping, and drawer clear behavior
3. Updated source navigation links from Mangaka Chapters and Editor ChapterReviewDetail to use slug

---

## Architecture path

```
URL identity:    series slug (user-facing)
UI state:        PublicationScheduleSeriesSuggestion (autocomplete + internal id)
API/filter:      seriesId (internal identity)
```

Clean Architecture flow for new slug/ID resolvers:
```
Blazor Web → IPublicationScheduleApiClient (typed) → HTTP → PublicationScheduleController
→ IPublicationScheduleRepository (EF AsNoTracking projection) → SQL Server
```

---

## Backend additions

| Layer | File | Change |
|---|---|---|
| Domain | `Domain/Interfaces/IPublicationScheduleRepository.cs` | Added `SeriesSlug` to `PublicationScheduleSeriesSuggestion` record; added `GetSeriesSuggestionBySlugAsync` and `GetSeriesSuggestionByIdAsync` to interface |
| Infrastructure | `Infrastructure/Repositories/PublicationScheduleRepository.cs` | Added slug projection to existing `GetSeriesSuggestionsAsync`; implemented 2 new lightweight EF resolution methods |
| API | `API/Controllers/Publication/PublicationScheduleController.cs` | Added `GET /api/publication/schedule/series/by-slug/{slug}` and `GET /api/publication/schedule/series/by-id/{id}` |
| Web Client | `Web/Services/Api/IPublicationScheduleApiClient.cs` | Added `GetSeriesSuggestionBySlugAsync` and `GetSeriesSuggestionByIdAsync` |
| Web Client | `Web/Services/Api/PublicationScheduleApiClient.cs` | Implemented 2 new client methods |
| Application | `Application/DTOs/Manga/MangakaChapterDtos.cs` | Added `string? SeriesSlug` to `MangakaChapterListItemDto` |
| Infrastructure | `Infrastructure/Repositories/MangakaChapterRepository.cs` | Updated DTO construction to include `chapter.Series?.Slug` |

**All backend queries are lightweight EF AsNoTracking projections.** No stored procedures. No new feature areas.

---

## Web fixes

| File | Change |
|---|---|
| `Web/Helpers/PublicationScheduleNavigation.cs` | `BuildUrl` now accepts `string? seriesSlug`; writes `?series={slug}` when available, falls back to `?seriesId={guid}` for legacy |
| `Web/Components/Pages/Publication/ScheduleCalendar.razor` | Added `_selectedSeriesSlug` field; parses `?series={slug}` from URL with `?seriesId={guid}` legacy fallback; resolves slug/ID via new API client methods; writes slug in generated URLs; fixes Bug 1 (autocomplete label) |
| `Web/Components/Pages/Publication/PublicationScheduleActionDrawer.razor` | Bug 2: Removed premature `_lastLoadedSeriesId = null` from `ClearSelectedSeriesAsync`; Bug 3: Added `GetSeriesScopedChapters()`, scoped `SearchChaptersAsync` to active series; Bug 4: Unconditional `ClearChapterSearch()` on series change |
| `Web/Components/Pages/Mangaka/Chapters.razor` | Updated `NavigateToCalendar` to use slug via `PublicationScheduleNavigation.BuildUrl` |
| `Web/Components/Pages/Editor/ChapterReviewDetail.razor` | Updated `NavigateToSchedule` to use slug via `PublicationScheduleNavigation.BuildUrl` |

---

## Root causes fixed

### Bug 1 — Parent autocomplete blank on URL load
**Fix:** `ResolveSelectedSeriesBySlugAsync` calls new `GET /api/publication/schedule/series/by-slug/{slug}` → populates `_selectedSuggestion` with `SeriesTitle` → MudAutocomplete displays name. Legacy `seriesId` URLs resolved via `ResolveLegacySelectedSeriesByIdAsync`.

### Bug 2 — Drawer chapters do not unfilter after drawer chip X
**Fix:** Removed premature `_lastLoadedSeriesId = null` from `ClearSelectedSeriesAsync`. Parent clear propagates `SelectedSeriesId = null` → `ShouldReloadChaptersForSeriesChange(null)` → `null != oldSeriesId` → TRUE → reload/refilter executes.

### Bug 3 — Chapter search suggestions show cross-series chapters
**Fix:** `SearchChaptersAsync` now uses `GetSeriesScopedChapters()` which filters `_allChapters` by `_selectedSeriesId` when a series filter is active.

### Bug 4 — Drawer chapter list does not auto-refresh
**Fix:** `OnParametersSetAsync` Mangaka path now unconditionally calls `ClearChapterSearch()` before `ApplyChapterFilters()` on any series change — prevents stale chapter selection from blocking the list.

---

## UI behavior changed

- **URL now uses slug:** Selecting a series writes `?series={slug}`, not `?seriesId={guid}`
- **Legacy URL support:** `?seriesId={guid}` still works — resolves to suggestion, subsequent navigation writes slug
- **Parent autocomplete** now displays series name on URL load (slug or legacy ID)
- **Drawer chip X** now correctly unfilters both calendar and drawer
- **Chapter search** scoped to active series filter
- **Stale chapter selection** cleared on series filter change
- **Source navigation** (Mangaka Chapters, Editor ChapterReviewDetail) now links with slug

---

## Verification

### Build
```
dotnet build .\MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
Result: **SUCCESS** — 0 errors, 66 warnings (all pre-existing)

### Manual smoke
Not run (build-only verification).

---

## Known issues / follow-ups

1. If a legacy `?seriesId={guid}` URL is for a series not in `SERIALIZED` status, the resolution returns null — `_selectedSeriesId` is still set from URL but `_selectedSuggestion` stays null (autocomplete blank, filter icon visible). Acceptable legacy behavior.
2. No standalone `by-id` URL resolution fallback for navigation links that still pass raw GUIDs — all source pages now use the slug-aware `PublicationScheduleNavigation.BuildUrl`.

---

## Docs/Revision Handoff Path

`docs/revision/PublicationScheduling/2026-07-06-schedule-slug-filter-state-fixes.md`
