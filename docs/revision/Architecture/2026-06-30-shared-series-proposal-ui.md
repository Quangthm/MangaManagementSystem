# Shared Series Proposal UI — 2026-06-30

## Branch
`feature/Mangaka`

## Problem
Editor and Mangaka had separate proposal UI implementations. Editor had the cleaner table/detail UX, while Mangaka had better filters/sorting. Mangaka proposal detail used a modal instead of a full detail page.

## Scope
Refactored proposal list/detail UI toward shared components while preserving role-specific routes, authorization, and actions.

## Route Decision
Used shared components with role-specific routes:
- `/editor/proposals/{proposalId}`
- `/mangaka/proposals/{proposalId}`

A single shared URL was not used because:
- Editor requires `[Authorize(Roles = "Tantou Editor")]` — shows claim UI, review decision actions, active editor contributor state
- Mangaka requires `[Authorize(Roles = "Mangaka")]` — must never see Editor-only actions
- Backend authorization is role-specific: Editor API uses `X-Actor-User-Id` header, Mangaka API scopes by contributor membership
- Different layouts: `EditorLayout` vs `MangakaLayout`

## Shared Components

### SeriesProposalTable.razor
Location: `Web/Components/Shared/SeriesProposalTable.razor`

Parameters:
- `Items: IReadOnlyList<SeriesProposalListItemModel>` — web-layer projection model
- `DefaultStatusFilter: string` — default status chip label (e.g. "All", "Under Editorial Review")
- `ShowGenreFilter: bool` — show/hide genre multi-select filter
- `ShowTagFilter: bool` — show/hide tag multi-select filter
- `ShowReviewerColumns: bool` — show/hide Reviewer/Reviewed columns
- `RowClicked: EventCallback<Guid>` — row click handler (receives SeriesProposalId)
- `AvailableGenres: IReadOnlyList<GenreDto>` — distinct genres across all proposals
- `AvailableTags: IReadOnlyList<TagDto>` — distinct tags across all proposals

Features:
- Status filter chips (All, Under Editorial Review, Under Board Review, Revision Requested, Approved, Cancelled, Withdrawn)
- Genre multi-select filter (via ReferenceMultiSelectFilter)
- Tag multi-select filter (via ReferenceMultiSelectFilter)
- Search by title/series/submitter
- Sort dropdown (Recently Submitted, Reviewed Date, Proposal Title A-Z, Status)
- MudTable with Editor-style clean layout
- Clickable rows with chevron affordance
- Markup file indicator icon

### SeriesProposalDetailView.razor
Location: `Web/Components/Shared/SeriesProposalDetailView.razor`

Parameters:
- `Proposal: SeriesProposalDetailViewModel` — web-layer projection model (required)
- `Actions: RenderFragment?` — optional action buttons (Editor uses for claim/review decisions)
- `SidePanel: RenderFragment?` — optional side panel content (Editor uses for claim UI, decision actions)

Renders read-only content:
- Cover image with fallback
- Proposal title + series title + version
- Status chip + series status mismatch warning
- Series slug
- Genres and tags
- Synopsis (expandable if > 300 chars)
- Proposal file open link
- Editorial decision section (reviewer, comments, markup file)
- Review comments when no formal decision exists
- Markup file link
- Submission info (submitted by/at, reviewed by/at)
- Withdrawn alert when applicable

Does NOT render any Editor-only actions (claim, pass to board, request revision, cancel).

### Web-layer projection models
Location: `Web/Components/Shared/`

- `SeriesProposalListItemModel.cs` — shared table row model
- `SeriesProposalDetailViewModel.cs` — shared detail view model

Both Editor and Mangaka pages map their role-specific DTOs (`ProposalQueueItemDto`, `MangakaSeriesProposalDto`, `EditorProposalDetailDto`) to these models.

## Editor Behavior
- Route: `/editor/proposals` → `ProposalList.razor`
- Route: `/editor/proposals/{ProposalId:guid}` → `ProposalReviewDetail.razor`
- Default status filter: `UNDER_EDITORIAL_REVIEW`
- Row click navigates to `/editor/proposals/{proposalId}`
- Removed `Review` button — replaced by row click + chevron
- Editor detail uses `SeriesProposalDetailView` with `SidePanel` render fragment for:
  - Claim UI (including "Claim as Additional Editor" when other editors are active)
  - Active editor contributor state / warning
  - Decision actions: Pass to Board, Request Revision, Cancel Proposal
  - State mismatch warnings
- All review decision modals preserved
- Claim backend unchanged

## Mangaka Behavior
- Route: `/mangaka/proposals` → `Proposals.razor`
- Route: `/mangaka/proposals/{ProposalId:guid}` → `ProposalDetail.razor` (NEW)
- Default status filter: `All`
- Row click navigates to `/mangaka/proposals/{proposalId}`
- Removed old modal detail — replaced by full-page detail route
- Mangaka filtering/sorting preserved:
  - Status filter chips
  - Genre multi-select filter
  - Tag multi-select filter
  - Search by title/series/submitter
  - Sort dropdown
- Mangaka detail page:
  - Uses `MangakaLayout`
  - Loads detail through `IMangakaSeriesApiClient.GetMySeriesProposalDetailAsync`
  - Back navigation to `/mangaka/proposals`
  - Renders using `SeriesProposalDetailView` (no Actions/SidePanel)
  - Shows proposal files, comments, markup as read-only
  - No Editor-only actions

## Authorization
- Editor route: `[Authorize(Roles = "Tantou Editor")]` — API behind `EditorProposalApiClient` uses `X-Actor-User-Id` header, backend enforces Tantou Editor role via stored procedures
- Mangaka route: `[Authorize(Roles = "Mangaka")]` — API behind `MangakaSeriesApiClient` uses `X-Actor-User-Id` header, backend enforces Mangaka contributor membership
- New Mangaka detail query `GetMySeriesProposalDetailQueryHandler` scopes by actor's active Mangaka contributor membership (same predicate as `GetMySeriesProposalsQueryHandler`)
- Repository method `GetMySeriesProposalDetailAsync` enforces the identical contributor access check as `GetMySeriesProposalsAsync`

## Files Changed

### Application Layer
- `Application/Features/Mangaka/SeriesProposals/Queries/GetMySeriesProposalDetail/GetMySeriesProposalDetailQuery.cs` (NEW)
- `Application/Features/Mangaka/SeriesProposals/Queries/GetMySeriesProposalDetail/GetMySeriesProposalDetailQueryHandler.cs` (NEW)

### Domain Layer
- `Domain/Interfaces/ISeriesProposalRepository.cs` — added `GetMySeriesProposalDetailAsync` method

### Infrastructure Layer
- `Infrastructure/Repositories/SeriesProposalRepository.cs` — implemented `GetMySeriesProposalDetailAsync`

### API Layer
- `API/Controllers/MangakaSeriesController.cs` — added `GetMySeriesProposalDetailAsync` endpoint, added using
- `API/Controllers/MangakaSeriesController.cs` — updated GetMySeriesProposalsAsync doc comment

### Web Layer — API Clients
- `Web/Services/Api/IMangakaSeriesApiClient.cs` — added `GetMySeriesProposalDetailAsync` method
- `Web/Services/Api/MangakaSeriesApiClient.cs` — implemented `GetMySeriesProposalDetailAsync`

### Web Layer — Shared Components (NEW)
- `Web/Components/Shared/SeriesProposalTable.razor` — shared proposal list component
- `Web/Components/Shared/SeriesProposalDetailView.razor` — shared proposal detail display
- `Web/Components/Shared/SeriesProposalListItemModel.cs` — web-layer projection model
- `Web/Components/Shared/SeriesProposalDetailViewModel.cs` — web-layer projection model

### Web Layer — Pages (MODIFIED)
- `Web/Components/Pages/Editor/ProposalList.razor` — uses SeriesProposalTable, default UNDER_EDITORIAL_REVIEW, row-click nav
- `Web/Components/Pages/Editor/ProposalReviewDetail.razor` — uses SeriesProposalDetailView with SidePanel for Editor actions
- `Web/Components/Pages/Mangaka/Proposals.razor` — uses SeriesProposalTable, row-click nav to detail

### Web Layer — Pages (NEW)
- `Web/Components/Pages/Mangaka/ProposalDetail.razor` — new Mangaka proposal detail page

## Build Result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded.
```
0 errors, 0 warnings.

## Manual Test Checklist
- [ ] Open `/editor/proposals`
- [ ] Confirm default filter is Under Editorial Review
- [ ] Confirm table uses clean shared layout
- [ ] Confirm row click opens `/editor/proposals/{id}`
- [ ] Confirm Editor detail still shows claim/review actions when allowed
- [ ] Confirm active editor claim warning still works
- [ ] Open `/mangaka/proposals`
- [ ] Confirm filters/sorting still work
- [ ] Confirm table uses clean shared layout
- [ ] Confirm row click opens `/mangaka/proposals/{id}`
- [ ] Confirm Mangaka detail shows proposal files/comments/markup as read-only
- [ ] Confirm Mangaka detail does not show Editor-only actions
- [ ] Confirm Mangaka cannot access another Mangaka's proposal detail
- [ ] Confirm Editor route still requires Editor role

## Remaining Follow-ups
- Editor ProposalList loads ALL proposals and filters client-side (status filter was moved from API to component). The shared table handles filtering locally. This is acceptable since proposals are typically not massive.

## Follow-up Fix — Shared Proposal Table Regression Cleanup

### Problem
Runtime testing after the shared proposal UI refactor showed:
- Editor proposal queue did not default to Under Editorial Review (status code vs label mismatch).
- Shared table columns were inconsistent between Editor and Mangaka.
- Table pagination was missing.
- Status filter counts disappeared.
- Shared table visual styling regressed to generic black/white controls.
- Mangaka proposal detail did not load cover image.
- Editor proposal queue needed an optional filter for proposals belonging to the current editor's active series.

### Fix
- **Default status filtering**: Normalized to use status codes (`DefaultStatusFilterCode`). Editor passes `UNDER_EDITORIAL_REVIEW`, Mangaka passes `ALL`. Component internally maps codes to display labels but compares by code.
- **Status counts**: Each chip shows `Label (N)` where N = loaded items matching that status code, computed from all loaded items (before current status filtering).
- **Client-side pagination**: Added inside `SeriesProposalTable.razor`. Default `PageSize = 10`. Pagination after all filters/search/sort. Resets to page 1 on any filter/search/sort/items change. Shown only when total pages > 1.
- **Column alignment**: Base columns (Series, Proposal, Version, Submitted, Status, Markup, chevron) always shown. Optional columns via parameters: `ShowSubmitterColumn`, `ShowReviewerColumns`. Editor shows all; Mangaka hides submitter/reviewer.
- **Styling**: Restored polished component-scoped styling — rounded status chips, active chip highlight, card-like table wrapper, clean toolbar spacing, clickable rows with chevron.
- **Mangaka cover URL**: Added `.Include(sp => sp.Series).ThenInclude(s => s.CoverFile)` to the Mangaka detail repository query. Added `SeriesCoverUrl` field to `MangakaSeriesProposalDto`. Handler and ProposalDetail page now map the cover URL.
- **Editor My Series Only**: Added `CurrentEditorIsActiveContributor` field to `ProposalQueueItemDto`. Extended `GetEditorialProposalQueueQuery` with `ActorUserId`. Handler queries `IsActiveTantouEditorContributorAsync` per unique series ID. Controller reads actor ID from `X-Actor-User-Id` header. Editor ProposalList uses this flag to filter — no reviewer-history heuristic.

### Files Changed (regression fix only)
| Layer | File | Change |
|-------|------|--------|
| Application DTO | `DTOs/Manga/SeriesProposalDtos.cs` | Added `CurrentEditorIsActiveContributor` to `ProposalQueueItemDto`; added `SeriesCoverUrl` to `MangakaSeriesProposalDto` |
| Application Query | `Features/Editor/.../GetEditorialProposalQueueQuery.cs` | Added `ActorUserId` parameter |
| Application Handler | `Features/Editor/.../GetEditorialProposalQueueQueryHandler.cs` | Queries active contributor per series, maps flag to DTO |
| Application Handler | `Features/Mangaka/.../GetMySeriesProposalDetailQueryHandler.cs` | Maps `SeriesCoverUrl` |
| API Controller | `Controllers/Editor/EditorProposalsController.cs` | Reads actor ID from header, passes to query |
| Infrastructure | `Repositories/SeriesProposalRepository.cs` | Added `CoverFile` include to `GetMySeriesProposalDetailAsync` |
| Web Shared | `Components/Shared/SeriesProposalTable.razor` | Status code filtering, counts, pagination, column flags, scoped styling |
| Web Shared | `Components/Shared/SeriesProposalListItemModel.cs` | Added `SeriesId`, `CurrentActorIsActiveContributor` |
| Web Page | `Pages/Editor/ProposalList.razor` | Uses contributor flag, correct table params, My Series Only toggle |
| Web Page | `Pages/Mangaka/Proposals.razor` | Correct params, SeriesId mapping |
| Web Page | `Pages/Mangaka/ProposalDetail.razor` | Maps `SeriesCoverUrl` from DTO |

### Build Result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
0 errors, 52 warnings (all pre-existing)
```

### Manual Test Checklist
- [ ] Open `/editor/proposals`.
- [ ] Confirm Under Editorial Review is selected by default.
- [ ] Confirm status filters show counts.
- [ ] Confirm table styling is polished and consistent with the app.
- [ ] Confirm table paginates after filters/search/sort.
- [ ] Confirm row click still opens `/editor/proposals/{id}`.
- [ ] Confirm Editor can toggle `My Series Only`.
- [ ] Confirm `My Series Only` only shows proposals for active contributor series.
- [ ] Confirm global discovery remains default when toggle is off.
- [ ] Open `/mangaka/proposals`.
- [ ] Confirm All is selected by default.
- [ ] Confirm status filters show counts.
- [ ] Confirm genre/tag/search/sort still work.
- [ ] Confirm table columns align visually with Editor mode.
- [ ] Confirm table paginates after filters/search/sort.
- [ ] Open `/mangaka/proposals/{id}`.
- [ ] Confirm cover image loads when available.
- [ ] Confirm no Editor-only actions appear.

## Follow-up Fix — Proposal Version Literal Razor Text

### Problem
The shared proposal table and shared proposal detail page displayed literal Razor text instead of the actual proposal version number:
- Table Version column: `v@p.ProposalVersionNo`
- Detail subtitle: `v@Proposal.ProposalVersionNo`

This affected both Editor and Mangaka because both roles use the shared proposal components.

### Root cause
Razor markup placed `@` immediately after literal text without explicit expression boundaries (`@(...)`), so the UI rendered the C# property expression as plain text.

### Fix
- `SeriesProposalTable.razor`: changed `v@p.ProposalVersionNo` → `v@(p.ProposalVersionNo)`
- `SeriesProposalDetailView.razor`: changed `v@Proposal.ProposalVersionNo` → `v@(Proposal.ProposalVersionNo)`
- Verified no remaining bad literal patterns exist in the shared proposal components.

### Files changed
- `Web/Components/Shared/SeriesProposalTable.razor`
- `Web/Components/Shared/SeriesProposalDetailView.razor`

### Build Result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded. 0 errors.
```

### Manual Test Checklist
- [ ] Open `/editor/proposals` — Version column displays v1, v2, v3, etc.
- [ ] Open `/editor/proposals/{id}` — Detail subtitle displays "Proposal v1", "Proposal v2", etc.
- [ ] Open `/mangaka/proposals` — Version column displays v1, v2, v3, etc.
- [ ] Open `/mangaka/proposals/{id}` — Detail subtitle displays "Proposal v1", "Proposal v2", etc.
