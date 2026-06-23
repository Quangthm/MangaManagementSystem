# Link Mangaka Sidebar to Enhanced Review Submissions Page

**Date:** 2026-06-22
**Branch:** `feature/Mangaka`
**Scope:** Web navigation/UI routing only. No backend/API/DB/SP changes.

---

## Task Summary

Wired the "Assistant Review" sidebar item in the Mangaka dashboard (`/mangaka`) to navigate to the enhanced `/mangaka/review-submissions` page instead of showing the old embedded simple table.

---

## Root Cause

`MangakaDashboard.razor` uses an internal `_activeTab` string to control which content pane is shown. The `"review"` tab label "Assistant Review" set `_activeTab = "review"`, which rendered an old simple `MudTable` of assigned tasks (Title, Type, Assigned To, Status, Due columns only). Meanwhile, the Phase 1 enhanced page at `/mangaka/review-submissions` was unreachable from the dashboard sidebar.

---

## Files Changed

| Layer | File | Change |
|-------|------|--------|
| **Web** | `Components/Pages/Mangaka/MangakaDashboard.razor` | Changed `onclick` for nav buttons to call `HandleNavClick()` method; added `HandleNavClick()` that routes `"review"` tab to `Nav.NavigateTo("/mangaka/review-submissions")` |

**1 file, ~10 lines added.**

---

## Navigation Behavior

### Before
```
/mangaka sidebar "Assistant Review" click
  → _activeTab = "review"
  → renders old MudTable with Title/Type/Assigned To/Status/Due columns
  → no Series/Chapter/Page/Version context
  → no filters beyond status
```

### After
```
/mangaka sidebar "Assistant Review" click
  → Nav.NavigateTo("/mangaka/review-submissions")
  → renders enhanced ReviewSubmissions.razor page
  → "Review Assistant Submissions" heading
  → stat cards (Under Review, Assigned, Completed, Cancelled)
  → filter bar (series search, task type, assistant, status)
  → task cards with Series/Chapter/Page/Version/Assigned/Priority/Due/Compensation/Regions
  → Approve/Return/Cancel action buttons
```

---

## Route Used

`/mangaka/review-submissions` (unchanged from Phase 1)

Sidebar label: **"Assistant Review"** (unchanged)

---

## Layout Consistency

- `/mangaka/review-submissions` uses `<MangakaLayout>` which is the standard Mangaka shell
- The dashboard `/mangaka` uses a custom inline shell
- Navigating to `/mangaka/review-submissions` takes the user to a full-width page under `<MangakaLayout>`
- "Current Series" and "Series Proposals" tabs still work as before (internal tab state)
- Direct navigation to `/mangaka/review-submissions` still works

---

## Backend/API/DB/SP Impact

**None.** This is a pure Web/UI routing change. No API endpoints, application services, infrastructure repositories, stored procedures, DTOs, or database changes.

---

## Verification

### Build

```
dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental
```

Result:
```
Build succeeded.
0 Errors
57 Warnings (all pre-existing, none from changed file)
```

### Manual Smoke Checklist

- [ ] Open `/mangaka`
- [ ] Sidebar shows "Assistant Review" label
- [ ] Click sidebar "Assistant Review"
- [ ] Browser navigates to `/mangaka/review-submissions`
- [ ] Enhanced "Review Assistant Submissions" page appears
- [ ] Old simple Assigned Tasks table is no longer the destination
- [ ] "Current Series" sidebar tab still navigates to Current Series view
- [ ] "Series Proposals" sidebar tab still navigates to Series Proposals view
- [ ] Direct navigation to `/mangaka/review-submissions` still works
- [ ] No backend/API behavior changed

---

## Change Detail

In `MangakaDashboard.razor`:

1. Changed sidebar button `onclick` from `() => _activeTab = nav.Id` to `@(() => HandleNavClick(nav.Id))`
2. Added `HandleNavClick(string navId)` method:

```csharp
private void HandleNavClick(string navId)
{
    if (navId == "review")
    {
        Nav.NavigateTo("/mangaka/review-submissions");
    }
    else
    {
        _activeTab = navId;
    }
}
```

The old `else if (_activeTab == "review")` block (lines ~622-659) is now unreachable dead code since the review tab immediately navigates away. It is intentionally left in place for minimal change scope per task instructions.

---

## Follow-up

- Consider renaming sidebar label from "Assistant Review" to "Task Management" when Phase 2/3 are implemented
- Consider unifying the dashboard shell layout with `<MangakaLayout>` for consistency
