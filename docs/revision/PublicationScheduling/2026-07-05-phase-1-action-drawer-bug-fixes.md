# Phase 1 Action Drawer Bug Fixes & Mangaka Chapters Cleanup

**Date:** 2026-07-05
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors)

---

## Summary

Three fixes:
1. **Removed legacy "Schedule Release" UI from Mangaka/Chapters page** — scheduling is now centralized through `/publication/schedule`. Replaced with prominent "Manage in Calendar" button for APPROVED/SCHEDULED/ON_HOLD chapters.
2. **Fixed Manage Schedule drawer reopen after outside-click** — changed `Open="@_drawerOpen"` to `@bind-Open="_drawerOpen"` so MudDrawer properly syncs its internal state with the parent.
3. **Fixed Editor drawer "An unexpected error occurred"** — API client was calling wrong route `api/editor/series-chapters`. Corrected to `api/editor/chapters/series-chapters` to match the controller's `[Route("api/editor/chapters")]` + `[HttpGet("series-chapters")]`.

---

## Files Changed

| File | Change |
|---|---|
| `Web/Components/Pages/Mangaka/Chapters.razor` | Removed APPROVED "Schedule Release" button, schedule dialog HTML, schedule dialog state fields, `OpenScheduleDialog`/`ScheduleChapter` methods (~47 lines removed). "View Calendar" button now shows as filled primary "Manage in Calendar" for APPROVED/SCHEDULED/ON_HOLD chapters. |
| `Web/Components/Pages/Publication/ScheduleCalendar.razor` | Changed `MudDrawer Open="_drawerOpen"` → `@bind-Open="_drawerOpen"` to fix two-way binding on outside-click close. |
| `Web/Services/Api/EditorChapterReviewApiClient.cs` | Fixed route: `api/editor/series-chapters` → `api/editor/chapters/series-chapters` |

---

## Issue 1: Mangaka/Chapters Legacy Schedule UI

**Before:** APPROVED chapters showed a "Schedule Release" button that opened an inline MudDialog with a date picker and "Schedule" action. This duplicated the scheduling UI now centralized in the Publication Schedule drawer.

**After:** APPROVED chapters no longer have a "Schedule Release" button. Instead, all non-CANCELLED/non-RELEASED chapters show a calendar navigation button:
- **APPROVED/SCHEDULED/ON_HOLD**: Filled primary button "Manage in Calendar"
- **Other statuses**: Outlined secondary button "View Calendar"

Both navigate to `/publication/schedule?seriesId={seriesId}&anchorDate={plannedReleaseDate|today}`.

**Removed:**
- Schedule dialog HTML (30 lines)
- `_showScheduleDialog`, `_scheduleChapter`, `_scheduleDate` state fields
- `OpenScheduleDialog()` method
- `ScheduleChapter()` method (including `ScheduleApprovedChapterRequest` usage)

---

## Issue 2: Manage Schedule Drawer Reopen Bug

**Root cause:** `MudDrawer Open="@_drawerOpen"` is one-way binding. When the user clicks outside the drawer, MudDrawer closes itself internally (via its `Temporary` variant overlay), but `_drawerOpen` remains `true` in the parent component. When the user clicks "Manage Schedule" again, `OpenDrawer()` sees `_drawerOpen == true` and just calls `StateHasChanged()` — the drawer is already invisible at the Blazor level so nothing happens.

**Fix:** Changed to `@bind-Open="_drawerOpen"`. MudBlazor's `@bind-Open` properly calls the `OpenChanged` callback with `false` when the drawer closes internally via overlay click. The parent's `_drawerOpen` is updated to `false`, and the next button click sets it to `true` correctly.

---

## Issue 3: Editor Drawer Error

**Root cause:** API client route mismatch.
- Controller: `[Route("api/editor/chapters")]` + `[HttpGet("series-chapters")]` = `/api/editor/chapters/series-chapters`
- API client was calling: `api/editor/series-chapters` (missing the `/chapters` segment)

This produced a 404 from the API, which the client handled as a generic `InvalidOperationException`, showing "An unexpected error occurred. Please try again." in the drawer.

**Fix:** Changed `StringBuilder("api/editor/series-chapters?")` → `StringBuilder("api/editor/chapters/series-chapters?")`.

---

## Build Result

**SUCCESS** — 0 errors

---

## Runtime Smoke Test Status

Not run (build-only verification).

After runtime verification:
- [ ] Mangaka/Chapters page no longer shows "Schedule Release" for APPROVED chapters
- [ ] Mangaka/Chapters shows "Manage in Calendar" (filled primary) for APPROVED/SCHEDULED/ON_HOLD
- [ ] Mangaka/Chapters shows "View Calendar" (outlined) for DRAFT/REVISION_REQUESTED/UNDER_REVIEW
- [ ] "Manage in Calendar" / "View Calendar" navigates to `/publication/schedule` with correct query params
- [ ] Manage Schedule drawer opens on first click
- [ ] Drawer closes on X button
- [ ] Drawer closes on outside-click
- [ ] Drawer reopens after outside-click close
- [ ] Editor drawer loads chapters without error
- [ ] Editor drawer shows all actionable statuses (DRAFT, REVISION_REQUESTED, UNDER_REVIEW, APPROVED, SCHEDULED, ON_HOLD)
- [ ] Editor can schedule, hold, release chapters from drawer

---

## Remaining Gaps

1. Mangaka cover images in drawer still show "N/C" placeholder (`MangakaChapterListItemDto` lacks `SeriesCoverUrl`)
2. Server-side `searchText` parameter available but not wired from drawer
3. `CanSchedule`/`CanPutOnHold`/`CanRelease` booleans from backend DTO not yet used

---

## Phase 2 Reminder

Drag-and-drop from drawer cards to calendar day columns. Not part of this task.

---

## Docs/Revision Handoff Path

`docs/revision/PublicationScheduling/2026-07-05-phase-1-action-drawer-bug-fixes.md`
