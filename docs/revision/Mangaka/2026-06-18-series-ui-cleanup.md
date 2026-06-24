# Series UI Cleanup Patch

**Date:** 2026-06-18
**Branch:** feature/Mangaka
**Author:** Claude (AI Assistant)

## Summary

Focused UI cleanup patch addressing modal layering, sidebar cleanup, series detail page UI, and contributor panel.

## Changes

### 1. Modal/menu layering fix (`MangakaDashboard.razor`)

- Replaced `MudMenu` (MudBlazor popover portal) with a custom local dropdown div for the card kebab "..." button.
- Custom dropdown uses `_openSeriesActionMenuId` tracking variable to manage open/close state.
- Dropdown closes before opening any modal (`_openSeriesActionMenuId = null` in all modal openers).
- No MudBlazor popover portal involved — the dropdown div lives inside the card DOM, so it stays behind any `MudOverlay`.

### 2. Removed Ranking Tracker and Audit Log from Mangaka sidebar (`MangakaDashboard.razor`)

- Removed `"ranking"` and `"audit"` entries from `_navItems`.
- Removed the ranking tab content block (mock data: "Update Weekly Poll Results", "Prepare Board Review", `RankingRow` record, `_rankingData`, `_rankingWeek`, `_pollWeek/Series/Votes/Rank/Notes`, `_showRankingUpdate`).
- Removed the audit tab placeholder content.
- Simplified header subtitle to static text.

### 3. SeriesPage.razor UI cleanup

- Removed "Stable series URL" section.
- Moved genre from inline badge to its own "Genres" section with label.
- Header contributor section now shows only **active Mangaka** (filtered by `RoleName == "Mangaka" && EndDate == null`).
- Added "View all contributors" button that opens a `MudDialog`.
- Contributor dialog shows **Active Contributors** and **Past Contributors** grouped with role, start date, end date.

### 4. Contributor read-model changes

- Created `Domain/ReadModels/SeriesContributorReadModel` — repository returns structured contributor data.
- Added `Application/DTOs/Manga/SeriesContributorSummaryDto` — public API DTO.
- Updated `SeriesDetailDto` — replaced `IReadOnlyList<string> ContributorDisplayNames` with `IReadOnlyList<SeriesContributorSummaryDto> Contributors`.
- Updated `SeriesRepository.GetSeriesDetailBySlugAsync` — query now returns all contributors (active + past) with `DisplayName`, `RoleName`, `StartDate`, `EndDate`, using `.Select()` projection through `User.Role.RoleName`.
- Updated `GetSeriesBySlugQueryHandler` — maps from read model to DTO.

## MudDialog decision

Used MudDialog for the contributor panel. MudDialogProvider is already configured in App.razor (line 17) and IDialogService is registered via AddMudServices(). Clean approach with no additional setup needed.

## Files changed

| File | Change |
|------|--------|
| `src/MangaManagementSystem.Domain/ReadModels/SeriesContributorReadModel.cs` | **New** — read model for repository contributor results |
| `src/MangaManagementSystem.Domain/Interfaces/ISeriesRepository.cs` | Updated return type from `IReadOnlyList<string>` to `IReadOnlyList<SeriesContributorReadModel>` |
| `src/MangaManagementSystem.Application/DTOs/Manga/SeriesDetailDtos.cs` | Added `SeriesContributorSummaryDto`, updated `SeriesDetailDto.Contributors` type |
| `src/MangaManagementSystem.Application/Features/Series/Queries/GetSeriesBySlug/GetSeriesBySlugQueryHandler.cs` | Updated mapping from read model to DTO |
| `src/MangaManagementSystem.Infrastructure/Repositories/SeriesRepository.cs` | Updated contributor query to include role/start/end dates for all contributors |
| `src/MangaManagementSystem.Web/Components/Pages/Series/SeriesPage.razor` | Removed URL section, restructured genre, updated contributor display, added MudDialog panel |
| `src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor` | Replaced MudMenu with custom dropdown, removed ranking/audit tabs and state |

## Not changed

- `/series/{slug}/workspace` route and workspace internals
- Create Draft / Edit Draft / Submit Proposal / Cancel Draft / Review Status workflows
- ISeriesApiClient / SeriesApiClient
- SeriesController
- Auth/Admin/Board
- NavMenu.razor or any layout files
- Workspace internals, AI/OCR, task workflow, annotation workflow, chapter/page upload

## Build result

`dotnet build MangaManagementSystem.slnx` — **0 errors, all pre-existing warnings only**.
