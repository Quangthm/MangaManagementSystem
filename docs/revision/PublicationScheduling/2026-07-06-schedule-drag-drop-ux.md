# Schedule Drag/Drop Scheduling UX

**Date:** 2026-07-06
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors, 66 pre-existing warnings, none from changed files)

---

## Task summary

Added drag-and-drop scheduling UX to the Publication Schedule page. Users can drag actionable chapter cards from the Manage Schedule drawer and drop them onto future calendar date cells to schedule (or reschedule) chapters — without opening the schedule dialog.

---

## Architecture path

```
Web only — no API, Application, Infrastructure, or database changes.

Drag source: PublicationScheduleActionDrawer (chapter cards)
  → EventCallback<ActionableChapterItem> → ScheduleCalendar (drag state)
  → Drop target: day column <div> in ScheduleCalendar
  → ScheduleChapterFromDropAsync()
    → MangakaApi.SetPlannedReleaseDateAsync / EditorApi.SetPlannedReleaseDateAsync
    → LoadSchedule() + DrawerReloadVersion++
```

---

## Files changed

| File | Change |
|---|---|
| `Web/Components/Pages/Publication/PublicationScheduleActionDrawer.razor` | Added drag source (`draggable`, `@ondragstart`, `@ondragend`), `OnChapterDragStarted`/`OnChapterDragEnded` EventCallbacks, `DrawerReloadVersion` parameter with version-check reload |
| `Web/Components/Pages/Publication/ScheduleCalendar.razor` | Added `IMangakaChapterApiClient`/`IEditorChapterReviewApiClient` injection, drag state fields (`_draggedChapter`, `_isDragging`, `_drawerReloadVersion`), drop target rendering (`@ondragover:preventDefault`, `@ondrop`, `.mf-drag-over` CSS), `ScheduleChapterFromDropAsync` dispatch method |

---

## Web/API/backend impact

**Web-only.** No API, Application, Infrastructure, or database changes. Uses existing `SetPlannedReleaseDateAsync` methods on both `IMangakaChapterApiClient` and `IEditorChapterReviewApiClient` — the same API methods used by the existing schedule/reschedule dialog flow.

---

## Existing schedule flow reused

The drop handler calls the exact same API:
- `MangakaApi.SetPlannedReleaseDateAsync(actorUserId, chapterId, new SetPlannedReleaseDateRequest(targetDate))`
- `EditorApi.SetPlannedReleaseDateAsync(actorUserId, chapterId, new SetPlannedReleaseDateRequest(targetDate))`

Returns `SetChapterPlannedReleaseDateResponse` with `ValidationMessage` (shown as success snackbar) and `WarningMessage` (shown as advisory snackbar).

---

## No-unschedule guarantee

| Guarantee | How |
|---|---|
| Calendar items not draggable | Only drawer chapter cards have `draggable` attribute |
| Drawer not a drop target | No `@ondragover`/`@ondrop` on drawer |
| No clear-date action | No new buttons or actions added |
| Can't bypass status/permission | `CanDragChapterForDrop` blocks RELEASED, CANCELLED, UNDER_REVIEW; drawer `CanDragChapter` wraps `CanSchedule` → blocks EXCLUDED statuses |
| Past dates blocked | `IsDateDropTargetEnabled(date)` → `date.Date >= DateTime.UtcNow.Date` |
| Null drag payload | `OnDropOnDate` returns immediately if `_draggedChapter == null` |
| Backend final authority | API validates status, permissions, date rules server-side |

---

## UI behavior

- Chapter cards in drawer show as draggable when `CanSchedule` returns true
- Dragging a chapter highlights all day columns with a blue glow
- Dropping on a future date schedules/reschedules the chapter
- Dropping on a past date shows a warning snackbar
- After successful drop: calendar refreshes, drawer reloads, slug/series filter preserved
- Non-draggable chapters (RELEASED, CANCELLED, UNDER_REVIEW) have no drag affordance
- Existing schedule/reschedule buttons and dialog remain functional as fallback
- Rescheduling: dragging a SCHEDULED chapter to a new date calls the same API → new planned_release_date

---

## Verification

### Build
```
dotnet build .\MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
Result: **SUCCESS** — 0 errors, 66 pre-existing warnings

### Manual smoke
Not run (build-only verification).

---

## Known issues / follow-ups

1. **Native HTML5 drag/drop not supported on mobile/touch devices.** Existing schedule buttons/dialogs remain as fallback. This is an enhancement, not the only scheduling path.
2. **Drop feedback is immediate** — no confirmation dialog. The drag action itself serves as intent. Cancel via snackbar error if API rejects.
3. **`CanDragChapterForDrop` duplicates parts of `CanSchedule`** but is intentionally simpler (no `_isMangaka` dependency) since it runs in the parent which has the same role as the drawer.

---

## Docs/Revision Handoff Path

`docs/revision/PublicationScheduling/2026-07-06-schedule-drag-drop-ux.md`
