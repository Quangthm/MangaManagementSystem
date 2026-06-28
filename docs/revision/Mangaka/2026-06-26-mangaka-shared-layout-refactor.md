# Mangaka Shared Layout Refactor — 2026-06-26

## Branch
`feature/Mangaka`

## Why Phase 1 was skipped
Phase 1 (single nav item into custom sidebar) was cancelled because Phase 2 layout migration would overwrite it. Instead, we skipped directly to a shared layout pattern.

## Scope
Web UI layout/navigation refactor only. No API/Application/Infrastructure/DB changes.

## Problem Summary
- `MangakaDashboard.razor` used a custom inline shell (260px sidebar, inline tabs, embedded profile) instead of `<MangakaLayout>`
- `MangakaLayout.razor` was a bare wrapper with no sidebar/nav
- `Chapters.razor` (`/mangaka/chapters`) worked but was undiscoverable from `/mangaka`
- No consistent sidebar navigation across Mangaka pages

## Layout Diagnosis
All 5 role layouts (Editor, Assistant, Admin, Dashboard, Ranking) share the exact same structure:
- `<div class="mf-shell">` / `<aside class="mf-sidebar mf-{role}-sidebar">`
- `<UserAvatarMenu />` profile area
- `<nav class="mf-nav-section">` with `<NavLink>` entries
- `<MudSpacer />` + "Back to Overview" button
- `<main class="mf-main">@ChildContent</main>`

They differ only in sidebar CSS class, nav title, and nav link entries.

## Files Changed

### Created
1. `Components/Layouts/RoleSidebarLayout.razor`
   - Shared layout shell component with parameterized sidebar CSS class, nav title, nav items, and overview href
   - Includes `RoleSidebarNavItem` record: `(string Href, string Label, string Icon, bool MatchAll)`
   - Contains scoped `<style>` for `mf-sidebar-profile-area` (removed duplication from all role layouts)
   - Supports `ExtraNavContent` parameter for role-specific nav sections (e.g. Editor's "Ranking & Context")

### Modified
2. `Components/Layouts/MangakaLayout.razor`
   - Replaced bare wrapper with `<RoleSidebarLayout>` using `mf-mangaka-sidebar` class
   - Mangaka nav links:
     - `/mangaka` → Dashboard (MatchAll)
     - `/mangaka/chapters` → Chapter Submissions
     - `/mangaka/review-submissions` → Assistant Review
     - `/mangaka/contributors` → Manage Contributors

3. `Components/Pages/Mangaka/MangakaDashboard.razor`
   - Wrapped content in `<MangakaLayout>`
   - Removed custom inline 260px sidebar (profile area, inline nav tabs)
   - Removed outer `mf-dashboard-shell` div and `main` wrapper
   - Moved top header and scrollable content inside `<MangakaLayout>`
   - Simplified `_navItems` from 4 items to 2 (only "Current Series" and "Series Proposals")
   - Simplified `HandleNavClick` to just set `_activeTab` (no navigation)
   - Removed dead "review" else branch from header subtitle
   - All modals/dialogs preserved unchanged
   - Profile now served by `UserAvatarMenu` in sidebar (replaced old inline profile)

## What Was NOT Changed
- `Chapters.razor`, `ReviewSubmissions.razor`, `ManageContributors.razor` — already used `<MangakaLayout>`, no changes needed
- `CreatorWorkspace.razor` — untouched
- `EditorLayout.razor`, `AssistantLayout.razor`, `AdminLayout.razor`, `DashboardLayout.razor`, `RankingLayout.razor` — deferred for future migration to `RoleSidebarLayout`
- `MangaFlowShell.razor` — untouched
- Global CSS (`mangaflow.css`, `app.css`) — untouched
- API/Application/Infrastructure/DB — untouched
- `SafeReturnUrl` and workspace `returnUrl` — untouched

## Mangaka Routes Now Visible in Sidebar
| Label | Route | Match |
|---|---|---|
| Dashboard | `/mangaka` | All |
| Chapter Submissions | `/mangaka/chapters` | Prefix |
| Assistant Review | `/mangaka/review-submissions` | Prefix |
| Manage Contributors | `/mangaka/contributors` | Prefix |
| Back to Overview | `/dashboard` | — |

## Build Result
```
Build succeeded.
    44 Warning(s) — all pre-existing (MUD0002, CS8602, CS0649, CS8601)
    0 Error(s)
```

## Known Issues
- `MangakaDashboard.razor` still contains `LoadCurrentUserSummaryAsync` and profile-related fields (`_currentAvatarUrl`, `_currentDisplayName`, etc.) that are now unused (profile is handled by `UserAvatarMenu`). These are harmless dead code — cleanup deferred.
- Scrollable content still uses `flex: 1` which works fine inside `mf-main` but could be simplified in future CSS cleanup.
- Series Proposals is still an inline tab within the dashboard page — no separate proposals route was created.

## Manual Test Checklist
- [ ] Open `/mangaka` — confirm sidebar appears with consistent layout
- [ ] Confirm dashboard shows "Current Series" and "Series Proposals" tabs
- [ ] Click "Chapter Submissions" in sidebar → opens `/mangaka/chapters`
- [ ] Click "Assistant Review" in sidebar → opens `/mangaka/review-submissions`  
- [ ] Click "Manage Contributors" → opens `/mangaka/contributors`
- [ ] Submit a chapter and confirm status becomes UNDER_REVIEW
- [ ] Navigate back to Mangaka dashboard from any page
- [ ] Open workspace from ReviewSubmissions and confirm returnUrl back-navigation
- [ ] Spot-check Editor/Assistant/Admin/Board layouts for regressions
- [ ] Confirm "Back to Overview" button navigates to `/dashboard`

## Follow-up Recommendations
1. **Migrate other role layouts to `RoleSidebarLayout`** — EditorLayout, AssistantLayout, AdminLayout, DashboardLayout, RankingLayout are nearly identical copies. Each would become a 15-line wrapper using `RoleSidebarLayout`. This eliminates ~500 lines of duplicated code across 5 files.
2. **Remove unused profile code from MangakaDashboard** — `LoadCurrentUserSummaryAsync`, `_currentAvatarUrl`, `_currentDisplayName`, `_currentRoleName`, `_currentAvatarInitial`, and `IProfileApiClient` injection.
3. **Extract Series Proposals to its own route** (optional) — currently an inline tab in dashboard.
4. **CSS cleanup** — move mangaflow.css board-specific selectors into scoped files.

## Resume Prompt
```
Continue from docs/revision/Mangaka/2026-06-26-mangaka-shared-layout-refactor.md.
Implement follow-up: migrate EditorLayout/AssistantLayout/AdminLayout/DashboardLayout/RankingLayout to use RoleSidebarLayout.
```
