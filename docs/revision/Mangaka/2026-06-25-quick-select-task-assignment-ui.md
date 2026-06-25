# Quick Select Task Assignment — Phase 4 Web UI

## Branch

`feature/Mangaka`

## Date

2026-06-25

## Task summary

Implemented Quick Select Task Assignment Web UI on `/mangaka/review-submissions`. Added a "Quick Select" button that opens a progressive dialog with series/chapter/pages/assistant selection, task detail inputs, default description + per-page overrides, and batch task creation. Uses the backend endpoints implemented in Phase 3.

No backend changes were made. No stored procedures were created.

## Scope

### In scope
- Typed Web API client methods for Quick Select endpoints (added to `IMangakaTaskApiClient` / `MangakaTaskApiClient`)
- Quick Select button on ReviewSubmissions page header
- Progressive dialog UI (series → chapter → pages → assistant → task details → descriptions → confirm)
- Multi-page selection with Select All/Clear
- Per-page description overrides via expandable panels
- Submit with loading/error/success states
- Post-submit task list refresh via existing `RefreshTasksAfterMutationAsync()`
- UI spec documentation update

### Out of scope
- Backend changes (already implemented in Phase 3)
- Stored procedures
- New dialog component file (inline in ReviewSubmissions.razor follows existing pattern)

## UI flow

1. **Quick Select button** — visible on `/mangaka/review-submissions` below page header (hidden while initial page is loading)
2. **Dialog opens** — `MudDialog` with `MaxWidth.Medium`
3. **Step 1: Select Series** — `MudSelect` loaded from existing `IMangakaSeriesApiClient.GetMySeriesAsync()`. Changing series clears chapter/pages/assistant selections and reloads chapters + assistants.
4. **Step 2: Select Chapter** — `MudSelect` visible after series selected. Uses `GET .../chapters/quick-select`. Shows chapter number, title, page count. Changing chapter clears page selections and reloads pages.
5. **Step 3: Select Pages** — `MudCheckBox` list visible after chapter selected. Uses `GET .../pages/quick-select`. Shows page number and current version. Select All / Clear buttons.
6. **Step 4: Select Assistant** — `MudSelect` visible after chapter selected. Uses `GET .../assistants/quick-select`. Shows DisplayName and Username.
7. **Step 5: Task Details** — `MudSelect` for task type, `MudTextField` for title prefix, `MudNumericField` for priority (1-5), `MudDatePicker` for due date, `MudNumericField` for compensation.
8. **Step 6: Description + Overrides** — `MudTextField` for default description (required). `MudExpansionPanels` with per-page override text fields (optional).
9. **Submit** — Calls `POST .../tasks/quick-select`. Button disabled while submitting. On success: closes dialog, snackbar `"Created N task(s)."`, calls `RefreshTasksAfterMutationAsync()`.

## Typed API client methods

Added to `IMangakaTaskApiClient` / `MangakaTaskApiClient`:

- `GetQuickSelectChaptersAsync(actorUserId, seriesId)` → `GET .../chapters/quick-select`
- `GetQuickSelectPagesAsync(chapterId)` → `GET .../pages/quick-select`
- `GetQuickSelectAssistantsAsync(actorUserId, seriesId)` → `GET .../assistants/quick-select`
- `QuickSelectAssignAsync(actorUserId, request)` → `POST .../tasks/quick-select`

## State handling

| State | Implementation |
|-------|---------------|
| Series loading | `_quickSelectLoadingSeries` → `MudProgressLinear` |
| Chapters loading | `_quickSelectLoadingChapters` → `MudProgressLinear` |
| Pages loading | `_quickSelectLoadingPages` → `MudProgressLinear` |
| Assistants loading | `_quickSelectLoadingAssistants` → `MudProgressLinear` |
| No chapters | `MudAlert` warning |
| No pages | `MudAlert` warning |
| No assistants | `MudAlert` warning |
| Submitting | `_quickSelectSubmitting` → button disabled, text "Creating..." |
| Submission error | `Snackbar` with error message |
| Success | `Snackbar` success, dialog closes, task list refreshes |

## Submission behavior

- Client-side guard: `CanQuickSelectSubmit()` checks all required fields before enabling submit button
- Submits via `MangakaTaskClient.QuickSelectAssignAsync`
- On success: closes dialog, shows snackbar `"Created {Count} task(s)."`, calls `RefreshTasksAfterMutationAsync()`
- On failure: shows snackbar with error message, dialog stays open
- Double-submit prevention: `_quickSelectSubmitting` flag disables button

## Refresh behavior

After successful Quick Select submission, `RefreshTasksAfterMutationAsync()`:
- Closes all dialogs (including Quick Select)
- Reloads full task list via API
- Refreshes stat cards (Under Review, Assigned, Completed, Cancelled counts)
- Calls `InvokeAsync(StateHasChanged)`
- Preserves existing filter state

## Existing behavior preserved

All existing functionality on `/mangaka/review-submissions` remains unchanged:
- Approve task
- Return for Rework
- Cancel task
- Reassign task
- Search/filter by series/chapter, task type, assistant, status
- Stat cards
- Task cards with workspace links

## Files changed

| Layer | File | Change |
|-------|------|--------|
| **Web** | `Services/Api/IMangakaTaskApiClient.cs` | Added 4 Quick Select method signatures |
| **Web** | `Services/Api/MangakaTaskApiClient.cs` | Implemented 4 Quick Select methods |
| **Web** | `Components/Pages/Mangaka/ReviewSubmissions.razor` | Added Quick Select button, dialog, state variables, and methods (injected `IMangakaSeriesApiClient`) |
| **Docs** | `ui-spec.md` | Updated section 6.z with UI implementation details |

## Build result

```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded.
0 Errors
47 Warnings (45 pre-existing + 2 new MUD0002 analyzer warnings for Checked/CheckedChanged on MudCheckBox — false positives matching existing MUD0002 pattern)
```

## Runtime smoke

Not run. Requires:
1. Manual PageRegion SQL constraints applied to database
2. Running API server + Web server
3. Valid Cloudinary credentials
4. Test data: series with Mangaka contributor, chapters with pages and page versions, Assistant contributors, page files in Cloudinary

## Known issues

- MUD0002 analyzer warnings for `Checked`/`CheckedChanged` on `MudCheckBox` — these are false positives from the MudBlazor analyzer, same pattern as existing `MaxHeight` warnings on `MudImage`
- `OnQuickSelectPageToggled` method removed (dead code after switching to inline lambda for CheckedChanged)

## Next step

Runtime smoke testing once database, Cloudinary, and test data are ready:
1. Open `/mangaka/review-submissions`
2. Click "Quick Select"
3. Select series → chapter → pages → assistant
4. Fill task details and description
5. Click "Create Tasks"
6. Verify tasks appear in list and stat cards update
7. Verify existing Approve/Return/Cancel/Reassign still work
