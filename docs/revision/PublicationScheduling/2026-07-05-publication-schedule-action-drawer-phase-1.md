# Publication Schedule Action Drawer — Phase 1

**Date:** 2026-07-05
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors, 0 warnings from changed/new files)

---

## Summary

Implemented Phase 1 frontend-only right-side slide-out action panel on `/publication/schedule`. The panel provides role-gated schedule/reschedule/hold/release actions for Mangaka and Tantou Editor users. Uses MudBlazor `MudDrawer` + `IDialogService` pattern. Reuses the existing compact publication schedule card style internally.

---

## Files Changed

### Created (5)
| File | Purpose |
|---|---|
| `Web/Components/Pages/Publication/ActionableChapterItem.cs` | Shared model `ActionableChapterItem` + dialog result records `ScheduleChapterDialogResult`, `HoldChapterDialogResult` |
| `Web/Components/Pages/Publication/ScheduleChapterDialog.razor` | Schedule/reschedule date-picker confirmation dialog with MudDatePicker + status-aware headers |
| `Web/Components/Pages/Publication/HoldChapterDialog.razor` | Put on hold confirmation dialog with required reason text + warning that planned date is preserved |
| `Web/Components/Pages/Publication/ReleaseChapterDialog.razor` | Release confirmation dialog with informational alert about what gets set |
| `Web/Components/Pages/Publication/PublicationScheduleActionDrawer.razor` | Main drawer component: chapter list, search, role-gated actions, orchestrates dialogs |

### Modified (3)
| File | Change |
|---|---|
| `Web/Services/Api/IEditorChapterReviewApiClient.cs` | Added `ReleaseChapterAsync` method signature |
| `Web/Services/Api/EditorChapterReviewApiClient.cs` | Added `ReleaseChapterAsync` implementation (calls `POST /api/editor/chapters/{id}/release`) |
| `Web/Components/Pages/Publication/ScheduleCalendar.razor` | Added `MudLayout` + `MudDrawer`, role check (`user.IsInRole`), "Manage Schedule" button for Mangaka/Editor, drawer integration |

---

## Components Added/Modified

### PublicationScheduleActionDrawer.razor
Right-side slide-out panel containing:
- **Header**: "Schedule Actions" title + role chip (Mangaka/Editor) + close button
- **Filters**: series filter chip (inherited from main calendar), chapter typeahead search
- **Chapter list**: compact cards with cover, series title, chapter number, status badge, planned date, action buttons
- **Loading/Error/Empty states**: separate from main calendar
- **Actions**: Schedule/Reschedule, Hold (Editor only), Release (Editor only)
- **Refresh**: after any successful action, reloads panel list + triggers parent calendar refresh

### ScheduleChapterDialog.razor
- `MudDatePicker` with MinDate = today
- Shows chapter summary (series, number, title, status, current planned date)
- ON_HOLD: header says "Return to Schedule", info alert about the transition
- SCHEDULED: header says "Reschedule Chapter"
- Other: header says "Schedule Chapter"
- Returns `ScheduleChapterDialogResult`

### HoldChapterDialog.razor
- Requires non-empty reason (max 1000 chars)
- Shows warning that planned release date is preserved
- Returns `HoldChapterDialogResult`

### ReleaseChapterDialog.razor
- Shows what happens: `released_at_utc` set, `planned_release_date` behavior
- Requires explicit confirmation click
- Returns `true` on confirm

---

## Role Visibility Behavior

- **Mangaka & Tantou Editor**: "Manage Schedule" button visible in week-nav header; opens drawer
- **All other roles**: No button shown; drawer inaccessible
- Inside drawer: Mangaka sees only schedule/reschedule; Editor sees schedule/reschedule + hold + release

---

## Chapter Scoping Source

| Role | Data Source | Scope |
|---|---|---|
| Mangaka | `IMangakaChapterApiClient.GetMyChaptersAsync(_currentUserId)` | All chapters from series where actor is active Mangaka contributor |
| Editor | `IEditorChapterReviewApiClient.GetReviewQueueAsync(_currentUserId, null)` | Review queue chapters from editor's series (limited — see Known Gaps) |

**Status filters (client-side):**
- Excluded: RELEASED, CANCELLED
- Included: DRAFT, REVISION_REQUESTED, UNDER_REVIEW, APPROVED, SCHEDULED, ON_HOLD

---

## Series Search + Drawer Filtering Behavior

1. Selecting a series in the main calendar's series typeahead → passed to drawer as `SelectedSeriesId` + `SelectedSeriesTitle`
2. Drawer shows `MudChip` "Series: {Title}" with close button
3. Chapter list filters to that series only
4. Clearing the chip returns to all accessible chapters
5. Closing/clearing the main series search also clears the drawer filter

---

## Chapter Typeahead Behavior

- Local client-side search over loaded `_allChapters` list
- Matches on `ChapterNumberLabel`, `ChapterTitle`, or combined
- Minimum 1 character to trigger search
- Returns max 10 results
- Selecting a result filters the drawer list to that chapter only
- Clearing resets to the full filtered list

---

## Mangaka Actions

| Chapter Status | Available Action |
|---|---|
| DRAFT | Schedule (planned release date) |
| REVISION_REQUESTED | Schedule |
| APPROVED | Schedule (→ SCHEDULED) |
| SCHEDULED | Reschedule |
| ON_HOLD | "Return to Schedule" (→ SCHEDULED) |
| UNDER_REVIEW | **Blocked** — "Schedule" button shown as disabled with tooltip |

Uses: `PUT /api/mangaka/chapters/{id}/planned-release-date`

---

## Editor Actions

| Chapter Status | Available Actions |
|---|---|
| DRAFT | Schedule |
| REVISION_REQUESTED | Schedule |
| UNDER_REVIEW | Schedule |
| APPROVED | Schedule, Release |
| SCHEDULED | Reschedule, Hold, Release |
| ON_HOLD | "Return to Schedule" |

Uses:
- Schedule: `PUT /api/editor/chapters/{id}/planned-release-date`
- Hold: `POST /api/editor/chapters/{id}/hold`
- Release: `POST /api/editor/chapters/{id}/release`

---

## Confirmation Dialogs

Every mutating action opens a MudDialog via `IDialogService.ShowAsync<T>`:
1. **Schedule**: date picker + confirm
2. **Reschedule**: date picker + confirm
3. **ON_HOLD recovery**: date picker + info alert + confirm
4. **Put on hold**: reason required + warning alert + confirm
5. **Release**: info alert + confirm

On success: Snackbar + refresh panel + refresh calendar. On error: Snackbar with backend message.

---

## API Endpoints / Client Methods Used

| Client | Method | Endpoint |
|---|---|---|
| `IMangakaChapterApiClient` | `GetMyChaptersAsync` | `GET /api/mangaka/chapters` |
| `IMangakaChapterApiClient` | `SetPlannedReleaseDateAsync` | `PUT /api/mangaka/chapters/{id}/planned-release-date` |
| `IEditorChapterReviewApiClient` | `GetReviewQueueAsync` | `GET /api/editor/chapters/review-queue` |
| `IEditorChapterReviewApiClient` | `SetPlannedReleaseDateAsync` | `PUT /api/editor/chapters/{id}/planned-release-date` |
| `IEditorChapterReviewApiClient` | `PutChapterOnHoldAsync` | `POST /api/editor/chapters/{id}/hold` |
| `IEditorChapterReviewApiClient` | `ReleaseChapterAsync` | `POST /api/editor/chapters/{id}/release` ← **NEW** |

---

## Build Result

**SUCCESS** — 0 errors, 0 warnings from changed/new files.

---

## Runtime Smoke Test Status

Not run (build-only verification).

### Smoke Checklist — Mangaka
- [ ] Log in as Mangaka, open `/publication/schedule`
- [ ] Confirm "Manage Schedule" button is visible
- [ ] Click button → drawer opens from right
- [ ] Confirm drawer shows only chapters from active contributor series
- [ ] Confirm RELEASED/CANCELLED chapters are not shown
- [ ] Confirm UNDER_REVIEW chapter shows disabled "Schedule" button
- [ ] Click Schedule on a DRAFT/APPROVED chapter → date dialog opens
- [ ] Select a future date, confirm → snackbar success, panel refreshes, calendar refreshes
- [ ] Close drawer, reopen → data still loaded

### Smoke Checklist — Editor
- [ ] Log in as Tantou Editor, open `/publication/schedule`
- [ ] Confirm "Manage Schedule" button is visible
- [ ] Confirm Editor role chip shown in drawer
- [ ] Confirm Hold button visible on SCHEDULED chapters
- [ ] Confirm Release button visible on SCHEDULED/APPROVED chapters
- [ ] Click Hold on SCHEDULED chapter → reason dialog opens
- [ ] Provide reason, confirm → snackbar success
- [ ] Click Release on APPROVED/SCHEDULED → confirmation dialog opens
- [ ] Confirm release → snackbar success

### Smoke Checklist — Non-Editor/Mangaka
- [ ] Log in as Admin/Board Member/Assistant
- [ ] Open `/publication/schedule`
- [ ] Confirm "Manage Schedule" button is HIDDEN
- [ ] Calendar functions normally

### Smoke Checklist — Series Filter + Search
- [ ] Select a series in main search → drawer shows series chip
- [ ] Drawer chapter list filters to that series
- [ ] Clear series in main search → drawer list returns to all chapters
- [ ] Type chapter number in drawer typeahead → suggestions appear
- [ ] Select a suggestion → list filters to that chapter
- [ ] Clear typeahead → full list returns

---

## Known Gaps

1. **Editor chapter list endpoint missing**: `IEditorChapterReviewApiClient.GetReviewQueueAsync` returns only UNDER_REVIEW chapters from the editor's series. It does NOT return DRAFT, APPROVED, SCHEDULED, or ON_HOLD chapters from the editor's other series. A new backend read endpoint is needed:
   ```
   GET /api/editor/series-chapters?status=
   ```
   Returns all chapters from series where the actor is an active Tantou Editor contributor, with `PlannedReleaseDate`, `StatusCode`, `ChapterTitle`, etc. in the DTO.

2. **No cover images in drawer for Editor**: `EditorChapterReviewQueueItemDto` does not include `SeriesCoverUrl`. The Mangaka path similarly has no `SeriesCoverUrl` in `MangakaChapterListItemDto`. Covers show "N/C" placeholder. A future DTO update could add cover URLs.

3. **Drawer chapter list does not auto-refresh when series filter changes**: Currently, the drawer loads data on first open. If the series filter changes while the drawer is open, the list filters client-side. But new chapters added to the selected series won't appear until the drawer is manually refreshed or reopened.

4. **No advisory warning display**: The backend now returns `SuggestedReleaseDate` and `WarningMessage` in `SetChapterPlannedReleaseDateResponse`, but these are not displayed in the dialog/Snackbar yet.

---

## Phase 2 Reminder

Phase 2 (drag-and-drop) is NOT part of this task. When implemented:
- Drag cards from drawer to calendar day columns
- Visual feedback during drag
- Drop triggers schedule action for that date
- Keep existing dialogs as fallback

---

## Docs/Revision Handoff Path

`docs/revision/PublicationScheduling/2026-07-05-publication-schedule-action-drawer-phase-1.md`
