# Proposal Review Back-Link & Layout Follow-up

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-19

## Problem

### Task A — Back label changed but click target did not navigate correctly

The previous fix only made the label dynamic (`_backLabel`). The click target used `OnClick="GoBack"` which called `Nav.NavigateTo(SafeReturnUrl.Resolve(returnUrl, "/editor/proposals"))`. While this expression was correct in code, using a C# click handler in Blazor Server for navigation can be unreliable. The fix: compute `_backHref` alongside `_backLabel` from the same resolved value, then use `Href="@_backHref"` on the `MudButton` (native browser navigation).

### Task B — Cover still wasted desktop space

Previous cover was `max-width: 320px` inside a `md="4"` column, leaving excess blank space on desktop. Increased to `width: min(100%, 420px); max-width: 420px` — fills available space up to 420px on large screens while shrinking on mobile.

## Files Changed

- `src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalReviewDetail.razor`

### Change 1 — Single-source back href + label

- Added `_backHref` field (default: `"/editor/proposals"`)
- `OnInitializedAsync` computes `_backHref = SafeReturnUrl.Resolve(returnUrl, "/editor/proposals")` then derives `_backLabel` from it
- Button changed from `OnClick="GoBack"` to `Href="@_backHref"` — native navigation ensures reliable click target
- Removed `GoBack()` method
- Label mapping:
  - `/editor/dashboard` → "Back to Dashboard"
  - `/editor/series` → "Back to Series"
  - all others → "Back to Queue"

### Change 2 — Cover size increase

- `max-width: 320px` → `width: min(100%, 420px); max-width: 420px`
- Aspect ratio unchanged (2:3)
- Grid columns unchanged (`cover md="4"`, center `md="5"`, right `md="3"`)
- Image styles unchanged (`object-fit: cover`)

## Build Result

- **Errors:** 0
- **Warnings:** 60 (all pre-existing)

## Changed-File Warning Check

Command:
```powershell
dotnet build ... --no-incremental 2>&1 |
    Select-String -Pattern "ProposalReviewDetail|SafeReturnUrl|EditorDashboard"
```

Result: Zero matches — no warnings from changed files.

## Manual Test Checklist

- [ ] From Editor Dashboard Recent Series → opens proposal detail → "Back to Dashboard" → click returns to `/editor/dashboard`.
- [ ] From Proposal Review Queue → opens proposal detail → "Back to Queue" → click returns to `/editor/proposals`.
- [ ] From Series Library → opens proposal detail → "Back to Series" → click returns to `/editor/series`.
- [ ] Unsafe returnUrl → label "Back to Queue", click returns to `/editor/proposals`.
- [ ] Cover is visibly larger on desktop (~420px wide).
- [ ] Cover remains 2:3, not distorted, `object-fit: cover`.
- [ ] Missing-cover placeholder uses same 420px box size.
- [ ] No duplicate caption/title under cover.
- [ ] Build: 0 errors, no new changed-file warnings.

## Remaining Tasks

1. **Cover crop-upload pipeline:** Implement crop-to-`1000×1500` before upload; no DB schema change needed.
2. **Data cleanup:** Inconsistent `PROPOSAL_DRAFT` series with `UNDER_EDITORIAL_REVIEW` proposal rows.
