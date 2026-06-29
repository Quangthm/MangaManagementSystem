# Role Sidebar Layout Migration — 2026-06-26

## Branch
`feature/Mangaka`

## Starting State
- `RoleSidebarLayout.razor` already existed from the Mangaka shared layout refactor (2026-06-26).
- `MangakaLayout.razor` already used `RoleSidebarLayout`.
- Other 5 role layouts (`EditorLayout`, `AssistantLayout`, `AdminLayout`, `DashboardLayout`, `RankingLayout`) duplicated the same shell/sidebar/profile/nav structure with ~100 lines each.

## Scope
Web layout refactor only. No API/Application/Infrastructure/Domain/DB/global CSS changes.

## Task Summary
Migrated all 5 duplicated role layout wrappers to the shared `RoleSidebarLayout` pattern, eliminating ~375 lines of duplicate code (net -243 lines).

## Files Changed

| File | Change | +/− |
|---|---|---|
| `Components/Layouts/RoleSidebarLayout.razor` | Added empty-icon check | +9 |
| `Components/Layouts/AssistantLayout.razor` | Migrated to shared shell | −43 net |
| `Components/Layouts/DashboardLayout.razor` | Migrated to shared shell | −41 net |
| `Components/Layouts/RankingLayout.razor` | Migrated to shared shell with ExtraNavContent | −33 net |
| `Components/Layouts/AdminLayout.razor` | Migrated to shared shell with ExtraNavContent | −32 net |
| `Components/Layouts/EditorLayout.razor` | Migrated to shared shell with ExtraNavContent | −34 net |

**Total: 6 files changed, +132 / −375 lines.**

## Layouts Migrated

### AssistantLayout
- Sidebar: `mf-assistant-sidebar`, Nav Title: "Main Menu"
- 3 nav links: Dashboard (/assistant), Assigned Tasks, Completed Work
- No extra nav content

### DashboardLayout
- Sidebar: `mf-dashboard-sidebar`, Nav Title: "Workspace"
- 4 nav links: Overview (/dashboard), Editorial Records, Studio Progress, Review Workspace
- No extra nav content

### RankingLayout
- Sidebar: `mf-ranking-sidebar`, Nav Title: "Editorial Board"
- 2 nav links: Dashboard (/ranking), Board Decision Workspace
- ExtraNavContent: "Not Available" section with 4 disabled items

### AdminLayout
- Sidebar: `mf-admin-sidebar`, Nav Title: "Main Navigation"
- 1 nav item: Dashboard (/admin)
- ExtraNavContent: User Accounts, User Approval (with AuthorizeView), Board Polls, "System" section with 2 disabled items

### EditorLayout
- Sidebar: `mf-editor-sidebar`, Nav Title: "Editorial Workflow"
- 5 nav links: Dashboard, Series Library, Proposals, Chapters, Annotations
- ExtraNavContent: "Ranking & Context" section with Series Ranking link

## Shared Layout Changes
- `RoleSidebarLayout.razor`: Added null/empty check for `item.Icon` — `<MudIcon>` only renders when icon string is non-empty, allowing layouts like Assistant and Dashboard to have iconless nav links.

## Navigation Preservation
For all migrated roles:
- All existing routes preserved exactly
- All labels, icons (Material "mdi-" prefix where originally used), and MatchAll/Prefix behavior preserved
- Sidebar profile area (`UserAvatarMenu`) preserved
- "Back to Overview" button preserved with original href
- Active nav highlighting preserved via NavLinkMatch
- Extra sections (Ranking & Context, System, Not Available) preserved via ExtraNavContent
- AuthorizeView wrapping for Admin-only links preserved

## What Was NOT Changed
- `MangakaLayout.razor` — no changes needed
- All Mangaka pages and routes
- All other role pages and routes
- Chapter/proposal/task/workspace workflows
- SafeReturnUrl
- API/Application/Infrastructure/Domain/DB
- Global CSS (`mangaflow.css`, `app.css`)
- `MangaFlowShell.razor`

## Build Result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded.
    44 Warning(s) — all pre-existing (MUD0002, CS8602, CS0649, CS8601)
    0 Error(s)
```

## Manual Test Checklist
- [ ] Open `/mangaka` pages — sidebar works
- [ ] Open Assistant routes — sidebar/nav/profile work
- [ ] Open Editor routes — sidebar/nav/profile/extra sections work
- [ ] Open Admin routes — sidebar/nav/profile/extra sections/AuthorizeView work
- [ ] Open Dashboard layout routes — sidebar/nav/profile work
- [ ] Open Ranking/Board routes — sidebar/nav/profile/extra sections work
- [ ] Confirm active nav highlighting
- [ ] Confirm "Back to Overview" navigates correctly
- [ ] Confirm profile menu/avatar shown in all sidebars
- [ ] Spot-check mobile width

## Remaining Follow-ups
- CSS cleanup/scoping (deferred)
- Runtime smoke test all role pages
