# Mangaka Proposals Route and Dashboard Cleanup — 2026-06-26

## Branch
`feature/Mangaka`

## Starting State
- `RoleSidebarLayout.razor` shared shell was created
- `MangakaLayout.razor` uses `RoleSidebarLayout` with sidebar navigation
- `MangakaDashboard.razor` was wrapped in `<MangakaLayout>`
- Series Proposals existed as an inline tab (`_activeTab == "proposals"`) inside the dashboard
- A dead "review" tab with task stats/table was still present but unreachable
- Profile loading code (`LoadCurrentUserSummaryAsync`, `IProfileApiClient`) was still in the dashboard

## New Route Added
`/mangaka/proposals` — Series Proposals tracking page

## Files Changed

### Created
1. `Components/Pages/Mangaka/Proposals.razor`
   - Route: `@page "/mangaka/proposals"` with `[Authorize(Roles = "Mangaka")]`
   - Wraps content in `<MangakaLayout>`
   - Contains the full Series Proposals UI extracted from the dashboard:
     - Status filter chips (All, Under Editorial Review, Under Board Review, etc.)
     - Search by title text field
     - Sort dropdown (Recently Submitted, Reviewed Date, Title A-Z, Status)
     - Genre/Tag multi-select filters with ReferenceMultiSelectFilter
     - Proposal table: Series, Proposal, Version, Status, Submitted, Markup
     - Pagination controls
     - Proposal Detail Modal (read-only): series link, submitted/reviewed info, withdrawn, genres, synopsis snapshot, comments, proposal/markup file downloads
   - Injects: `IMangakaSeriesApiClient`, `IReferenceDataApiClient`, `NavigationManager`, `ISnackbar`, `AuthenticationStateProvider`
   - Back to Dashboard navigation button
   - Own `_currentUserId`, `LoadProposalTrackingAsync`, `LoadReferenceDataAsync`, `OnInitializedAsync`
   - Own `_availableGenres`/`_availableTags` (loaded independently from dashboard)

### Modified
2. `Components/Layouts/MangakaLayout.razor`
   - Added nav link: `/mangaka/proposals` → "Series Proposals" (icon: `Icons.Material.Filled.Assignment`)
   - Current nav links: Dashboard, Series Proposals, Chapter Submissions, Assistant Review, Manage Contributors

3. `Components/Pages/Mangaka/MangakaDashboard.razor`
   **Removed from markup:**
   - Series Proposals inline tab (entire `else if (_activeTab == "proposals")` block: filter chips, search, sort, multi-select, table, pagination)
   - Dead "review" tab (task stats cards, assigned tasks table)
   - Proposal Detail Modal (moved to Proposals.razor)
   - `@if (_activeTab == "current")` wrapper around series content

   **Removed from code block:**
   - `_activeTab` field
   - `_navItems` list and `HandleNavClick()` method
   - `NavItem` record
   - All proposal tracking state: `_proposalDetailOpen`, `_selectedProposal`, `_proposalSelectedFilter`, `_proposalSearchText`, `_proposalSortMode`, `_proposalPage`, `ProposalPageSize`, `_proposalData`, `_proposalFilterGenreIds`, `_proposalFilterTagIds`
   - `ProposalFilterGroups`, `ProposalFilterCount`, `AllFilteredProposals`, `ProposalTotalPages`, `PagedProposals`, `ResetProposalPage`
   - `ProposalStatusLabel`, `ProposalStatusColor`, `ProposalEmptyStateMessage`, `OpenProposalDetail`, `LoadProposalTrackingAsync`
   - `FormatFileSize`
   - All task/review state: `_tasks`, `_taskStats`, `LoadTasksAsync`, `GetTaskChipColor`, `FormatDate`, `LanguageLabel`, `TaskStats` record
   - Profile code: `LoadCurrentUserSummaryAsync`, `_currentAvatarUrl`, `_currentDisplayName`, `_currentRoleName`, `_currentAvatarInitial`
   - `IProfileApiClient` injection (now handled by `UserAvatarMenu` in sidebar)

   **Simplified:**
   - `OnInitializedAsync` — only loads `_currentUserId`, `LoadSeriesAsync`, `LoadReferenceDataAsync`
   - Header — static "Current Series" title instead of tab-dependent `_navItems.First(n => n.Id == _activeTab).Label`

   **Kept intact:**
   - Series cards, filter/search/sort
   - New Series draft creation/editing
   - Submit proposal from draft
   - Cancel draft
   - Cover crop/preview
   - Review status modal
   - Genre/Tag pickers
   - All modal dialogs

## Dashboard/Profile Cleanup Completed
- `IProfileApiClient` injection removed
- `LoadCurrentUserSummaryAsync` removed
- `_currentAvatarUrl`, `_currentDisplayName`, `_currentRoleName`, `_currentAvatarInitial` removed
- `_activeTab` and all inline tab logic removed
- Dead review/task code removed
- `NavItem` and `TaskStats` records removed

## What Was NOT Changed
- API/Application/Infrastructure/Domain/DB — no backend changes
- `Chapters.razor`, `ReviewSubmissions.razor`, `ManageContributors.razor`, `CreatorWorkspace.razor`
- `EditorLayout.razor`, `AssistantLayout.razor`, `AdminLayout.razor`, `DashboardLayout.razor`, `RankingLayout.razor`
- `MangaFlowShell.razor`
- Global CSS (`mangaflow.css`, `app.css`)
- `SafeReturnUrl` and workspace `returnUrl`

## Build Result
```
Build succeeded.
    44 Warning(s) — all pre-existing (MUD0002, CS8602, CS0649, CS8601)
    0 Error(s)
```

## Manual Test Checklist
- [ ] Open `/mangaka` — confirm Current Series dashboard works
- [ ] Confirm "New Series" button works
- [ ] Click "Series Proposals" in sidebar → opens `/mangaka/proposals`
- [ ] Confirm proposal list/table renders with filters, search, sort
- [ ] Click a proposal → proposal detail modal opens with file download links
- [ ] Click "Chapter Submissions" → `/mangaka/chapters` opens
- [ ] Click "Assistant Review" → `/mangaka/review-submissions` opens
- [ ] Click "Manage Contributors" → `/mangaka/contributors` opens
- [ ] Confirm no broken Series Proposals tab remains on dashboard
- [ ] Confirm create/edit/submit series draft still works
- [ ] Confirm workspace returnUrl behavior is unchanged
- [ ] Confirm no extra profile API call from MangakaDashboard
- [ ] Spot-check Editor/Assistant/Admin/Board layouts for regressions

## Remaining Follow-ups
1. Migrate EditorLayout/AssistantLayout/AdminLayout/DashboardLayout/RankingLayout to `RoleSidebarLayout` (deferred)
2. CSS cleanup/scoping (deferred)
