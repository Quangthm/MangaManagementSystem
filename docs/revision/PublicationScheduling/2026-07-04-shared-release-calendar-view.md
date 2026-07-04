# Shared Release Calendar View

**Date:** 2026-07-04
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors, pre-existing warnings only)

---

## Summary

Implemented a shared publication release calendar page (`/publication/schedule`) for viewing planned/released chapter updates across all series. The calendar shows one week per page with date-based columns, chapter cards with cover/status/title, and search/filters. Wired from both the Editor Chapter Review page and Mangaka Chapters page. No scheduling actions are implemented yet — calendar is view-only with links from existing pages.

---

## Route Chosen: `/publication/schedule`

This matches the project convention of short, descriptive route segments. The page uses `@attribute [Authorize]` with no role restriction, making it accessible to all authenticated users.

---

## Files Created (8)

| Layer | File | Purpose |
|---|---|---|
| Application | `Features/Publication/Schedule/Queries/GetPublicationScheduleCalendar/PublicationScheduleCalendarDto.cs` | `PublicationScheduleCalendarDto`, `PublicationScheduleDayDto`, `PublicationScheduleItemDto` |
| Application | `Features/Publication/Schedule/Queries/GetPublicationScheduleCalendar/GetPublicationScheduleCalendarQuery.cs` | Query with AnchorDate, SearchText, SeriesId, StatusCode, FrequencyCode |
| Application | `Features/Publication/Schedule/Queries/GetPublicationScheduleCalendar/GetPublicationScheduleCalendarQueryHandler.cs` | Week calculation, day grouping, effective date logic |
| Domain | `Domain/Interfaces/IPublicationScheduleRepository.cs` | `IPublicationScheduleRepository` interface + `PublicationScheduleChapter`, `PublicationScheduleSeriesFilterItem` records |
| Infrastructure | `Infrastructure/Repositories/PublicationScheduleRepository.cs` | EF Core AsNoTracking query with Include/ThenInclude for Series.CoverFile |
| API | `API/Controllers/Publication/PublicationScheduleController.cs` | `GET /api/publication/schedule` + `GET /api/publication/schedule/filter-series` |
| Web | `Web/Services/Api/IPublicationScheduleApiClient.cs` | Typed client interface with `GetScheduleAsync` + `GetFilterSeriesAsync` |
| Web | `Web/Services/Api/PublicationScheduleApiClient.cs` | HttpClient implementation with query string building |

## Files Modified (6)

| Layer | File | Change |
|---|---|---|
| Infrastructure | `Infrastructure/DependencyInjection.cs` | Registered `IPublicationScheduleRepository` |
| Web | `Web/Program.cs` | Registered `IPublicationScheduleApiClient` typed HTTP client |
| Web | `Web/Components/Pages/Editor/ChapterReviewDetail.razor` | Enabled "Schedule Release" button; navigates to `/publication/schedule?seriesId=...&anchorDate=...` |
| Web | `Web/Components/Pages/Mangaka/Chapters.razor` | Added "View Calendar" button for non-CANCELLED, non-RELEASED chapters |
| Web | `Web/Components/Pages/Publication/ScheduleCalendar.razor` | New shared calendar page |

---

## API Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/publication/schedule?anchorDate=...&searchText=...&seriesId=...&statusCode=...&frequencyCode=...` | Returns week calendar with days and items |
| GET | `/api/publication/schedule/filter-series` | Returns distinct series (ID + title) that have scheduled/released chapters |

---

## Calendar Query Behavior

- Calculates Monday-to-Sunday week around anchor date (default = today)
- Includes chapters where `planned_release_date` or `released_at_utc` falls within the week
- Effective display date: `released_at_utc` preferred over `planned_release_date`
- Excludes CANCELLED by default (unless explicitly filtered by status)
- Excludes chapters with both dates null
- Uses EF Core `AsNoTracking()` with `.Include(c => c.Series).ThenInclude(s => s!.CoverFile)`
- Groups results by effective date into 7 day slots

---

## Calendar UI Behavior

### Week Pagination
- Default opens current week
- Chevron left/right navigates by +/- 7 days
- "Today" button returns to current week
- Week label: "Jun 30 — Jul 6, 2026"

### Date Columns
- 7-column responsive grid
- Each column header: "Mon, Jun 30" style label
- Today's column highlighted with blue border + "Today" chip
- Empty columns show "No releases"

### Chapter Cards
- Cover thumbnail (48×72px, "No Cover" placeholder if missing)
- Series title (truncated)
- Chapter number label
- Chapter title (italic, optional)
- Status chip (Scheduled/Released/On Hold/Approved)
- Date label ("Released" or "Planned: MMM d, yyyy")

### Search & Filters
- Text search: series title, chapter title, chapter number (Enter key triggers)
- Series dropdown: populated from `filter-series` API
- Status dropdown: All / Scheduled / Released / On Hold / Approved
- Frequency dropdown: All / Weekly / Monthly / Irregular
- All filter changes trigger API reload

---

## Editor/Mangaka Link Behavior

### Editor Chapter Review page
- "Schedule Release" button (in Publication Schedule card) now navigates to:
  `/publication/schedule?seriesId={seriesId}&anchorDate={plannedReleaseDate|today}`
- Card still hidden for CANCELLED/RELEASED chapters

### Mangaka Chapters page
- New "View Calendar" button added for chapters where status is NOT CANCELLED or RELEASED
- Navigates to same URL pattern with series ID and anchor date
- Uses `MangakaChapterListItemDto.SeriesId` and `.PlannedReleaseDate`

---

## Permission/Action Limitations
- Calendar page is view-only; no schedule/reschedule/hold actions
- `@attribute [Authorize]` allows all authenticated users
- Workflow action buttons will be added in a later prompt with backend permission checks

---

## Build Result
**SUCCESS** — 0 errors, pre-existing warnings only (none from changed/new files)

---

## Manual Smoke Checklist
- [ ] Open `/publication/schedule` as authenticated user
- [ ] Confirm current week loads with correct date range
- [ ] Confirm Previous/Next week navigation works
- [ ] Confirm Today button returns to current week
- [ ] Confirm date headers display for each day
- [ ] Confirm chapters appear under correct dates
- [ ] Confirm released chapters use released_at date
- [ ] Confirm planned chapters use planned_release_date
- [ ] Confirm series cover/title/chapter number/status render
- [ ] Confirm missing cover uses placeholder
- [ ] Confirm search filters results
- [ ] Confirm series dropdown filters results
- [ ] Confirm status filter works
- [ ] Confirm frequency filter works
- [ ] Confirm empty day state appears
- [ ] Confirm Editor "Schedule Release" button navigates to calendar
- [ ] Confirm Mangaka "View Calendar" button navigates to calendar
- [ ] Confirm CANCELLED/RELEASED chapters do not show schedule action link
- [ ] Confirm no actual scheduling mutation is performed from calendar

---

## Follow-ups
- Actual schedule/reschedule/hold actions from calendar not implemented
- Permission-gated workflow action buttons on calendar
- ON_HOLD recovery
- Release automation
- Public reader visibility

---

## Redesign (2026-07-04) — Typeahead Autocomplete, Serialized-Only Query, Simplified Cards

### Problem
The calendar query/UI used the wrong filter model:
- Separate Series dropdown required loading all scheduled series upfront
- Status filter encouraged broad chapter status queries beyond schedule-relevant chapters
- Search matched chapter title (not desired UX)
- Cards showed too much metadata (chapter title, detailed status+date labels)
- Query did not restrict to serialized series

### Fix

#### 1. Series dropdown → MudAutocomplete typeahead
- Replaced `MudSelect<T="Guid?">` with `MudAutocomplete<T="PublicationScheduleSeriesSuggestion">`
- New endpoint: `GET /api/publication/schedule/series-suggestions?searchText=...`
- Only returns serialized series (filtered by `Series.StatusCode == "SERIALIZED"`)
- Limited to top 10 results
- Debounced by MudBlazor naturally via search function

#### 2. Query tightened to serialized + schedule-relevant
Base query now enforces:
- `c.Series.StatusCode == "SERIALIZED"` — only serialized series
- `c.StatusCode != "CANCELLED"` — exclude cancelled
- Only chapters with `planned_release_date` or `released_at_utc` within the visible week
- Removed broad status filter (`searchText`/`statusCode` params removed from query)
- Removed `.Include(Series).ThenInclude(CoverFile)` — now uses direct projection with null-safe navigation

#### 3. Simplified card UI
Each card now shows only:
- Series cover thumbnail (with placeholder for null URL)
- Series title
- Chapter number label
- Small status badge: "Planned", "Released", "On Hold"

Removed from cards:
- Chapter title
- Detailed display date ("Planned: MMM d, yyyy")
- Broad status labels (only 3 schedule-relevant labels remain)

#### 4. Status filter removed
Status dropdown removed entirely. Calendar now shows schedule-relevant chapters by effective date automatically.

### Files Changed

| Layer | File | Change |
|---|---|---|
| Domain | `IPublicationScheduleRepository.cs` | New `PublicationScheduleSeriesSuggestion` record; removed `PublicationScheduleSeriesFilterItem`; removed `searchText`/`statusCode` params; removed `GetFilterSeriesAsync`; added `GetSeriesSuggestionsAsync`; removed `ChapterTitle` from schedule chapter |
| Infrastructure | `PublicationScheduleRepository.cs` | Added `SERIALIZED` series filter; removed chapter title/search/status query predicates; added `GetSeriesSuggestionsAsync`; removed `GetFilterSeriesAsync`; removed Include/ThenInclude for direct projection |
| Application | `PublicationScheduleCalendarDto.cs` | Removed `ChapterTitle`, `DisplayDate`; added `StatusBadgeLabel` |
| Application | `GetPublicationScheduleCalendarQuery.cs` | Removed `SearchText`, `StatusCode` params |
| Application | `GetPublicationScheduleCalendarQueryHandler.cs` | Added `GetStatusBadgeLabel`; removed `GetDisplayDate`; passes simplified params |
| API | `PublicationScheduleController.cs` | Removed `searchText`/`statusCode` params; added `GET /series-suggestions`; removed `GET /filter-series` |
| Web | `IPublicationScheduleApiClient.cs` | Removed `searchText`/`statusCode`/`GetFilterSeriesAsync`; added `GetSeriesSuggestionsAsync` |
| Web | `PublicationScheduleApiClient.cs` | Updated to match interface changes |
| Web | `ScheduleCalendar.razor` | Full rewrite: `MudAutocomplete` typeahead, simplified cards, removed status filter, better empty states |

### Build Result
**SUCCESS** — 0 errors (only harmless CS8669 auto-generated nullable warnings)

---

## Fix (2026-07-04) — MudSelect Type Mismatch Crash

### Problem
The `/publication/schedule` page crashed at runtime with:
```
Unable to cast object of type 'MudBlazor.MudSelect`1[System.Nullable`1[System.Guid]]'
  to type 'MudBlazor.MudSelect`1[System.Guid]'.
```

### Root Cause
`MudSelect T="Guid?"` (line 85) contained `MudSelectItem` children with implicit `T="Guid"` because `series.SeriesId` is a non-nullable `Guid`. MudBlazor requires the parent `MudSelect<T>` and all `MudSelectItem<T>` children to use the exact same generic type.

The Status and Frequency selects had the same pattern: `MudSelect T="string"` with `MudSelectItem` using nullable `(string?)null` values, risking similar mismatches.

### Fix
Aligned all MudSelect and MudSelectItem generic types:

| Control | Before | After |
|---|---|---|
| Series MudSelect | `T="Guid?"` with items `T="Guid"` (implicit) | `T="Guid?"` with items `T="Guid?"` (explicit) |
| Status MudSelect | `T="string"` with nullable items | `T="string?"` with items `T="string?"` |
| Frequency MudSelect | `T="string"` with nullable items | `T="string?"` with items `T="string?"` |

- Series items cast: `@((Guid?)series.SeriesId)` 
- Null items cast: `@((Guid?)null)`, `@((string?)null)`

### Also Verified
- Series dropdown now displays series title (not GUID) — the display was broken because the binding couldn't resolve
- Cover placeholder ("No Cover") for null/empty `SeriesCoverUrl` already correct
- Loading state already correctly handled in `finally` block

### Build Result
**SUCCESS** — 0 errors

### Files Changed
- `Web/Components/Pages/Publication/ScheduleCalendar.razor` — fixed MudSelect/MudSelectItem types
