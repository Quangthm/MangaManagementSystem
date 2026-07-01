# Editor Dashboard Scope and Proposal Claim State — 2026-06-30

## Branch
`feature/Mangaka`

## Problem
Runtime testing showed:
- Editor Dashboard counts were not scoped to the current editor's active contributed series.
- Different editors saw the same Chapters Under Review and Pending Annotations counts.
- Proposal Review detail showed `Unclaimed Proposal` even when another Tantou Editor was already an active contributor for the series.

## Scope
Fixed Editor Dashboard contributor-scoped metrics and Proposal Review detail active editor contributor state.

## Required Context Read
- `docs/agents/AGENTS.md`
- `docs/agents/SESSION_RULE.md`
- `docs/revision/Editor/2026-06-28-editor-chapter-review-quick-actions.md`
- `docs/revision/Mangaka/2026-06-29-ui-text-cleanup-and-chapter-audit.md`
- Targeted searches in business docs confirming:
  - Active contributor = `SeriesContributor.end_date IS NULL` (BR-SC-005)
  - Multiple Tantou Editors per series allowed (BR-SC-008: "database should not block multiple Tantou Editors")
  - First submission visible in editorial review queue (BR-PROP-024)
- `manga.usp_SeriesProposal_ClaimEditorialReview` SP (line 4667): *"Multiple Tantou Editors are allowed for the same series"*

## Dashboard Scoping

### Metrics scoped to active contributor series
- **Chapters Under Review**: `Chapter.status_code == UNDER_REVIEW AND Chapter.series_id IN ActiveEditorSeriesIds`
- **Pending Annotations**: unresolved annotations where linked regions → page version → chapter page → chapter → series_id IN ActiveEditorSeriesIds
- **Serialized Series**: already scoped (unchanged)

### Active editor series IDs query
```csharp
ActiveSeriesContributors
    .Where(asc => asc.UserId == actorUserId)
    .Select(asc => asc.SeriesId)
```

### Pending Proposals decision
Kept globally claimable. Pending Proposals is a discovery queue for all active Tantou Editors, not assigned work. Editors must see unclaimed proposals to claim them. The `ProposalReviewQueue` preview also remains global.

## Proposal Claim State

### Active Tantou Editor contributor detection
New `ActiveTantouEditorInfo` domain record returned by `ISeriesProposalRepository.GetActiveTantouEditorContributorsAsync`. Queries `SeriesContributors` where `EndDate IS NULL`, User ACTIVE, role = Tantou Editor, joined with Users for display name.

### New DTO fields
- `ActiveTantouEditors: IReadOnlyList<ProposalActiveEditorContributorDto>` — all active Tantou Editors for the series
- `HasOtherActiveTantouEditor: bool` — at least one active Tantou Editor that is not the current actor

### UI states

| State | Condition | Display |
|-------|-----------|---------|
| Unclaimed | `CanClaim && !HasOtherActiveTantouEditor` | "Unclaimed Proposal" + "Claim Review" button |
| Other editor active | `CanClaim && HasOtherActiveTantouEditor` | "Active editor already assigned" listing other editors + "Claim as Additional Editor" |
| Current editor active | `CurrentActorIsActiveTantouEditorContributor` | "You are an active Tantou Editor for this series." + decision actions |

### Additional editor confirmation
Clicking "Claim as Additional Editor" opens a confirmation dialog:
- Text: "Another Tantou Editor is already active on this series. Claiming will add you as an additional active editor. Continue?"
- Cancel button closes dialog
- "Claim Anyway" button calls the existing claim endpoint

### Backend claim behavior
Verified: `manga.usp_SeriesProposal_ClaimEditorialReview` already allows multiple different Tantou Editors per series (line 4667). Duplicate active contributor for same user is blocked by table constraint (line 4668-4669). No backend changes needed.

## Files Changed

| Layer | File | Change |
|-------|------|--------|
| Domain | `Domain/Interfaces/ISeriesProposalRepository.cs` | Added `ActiveTantouEditorInfo` record and `GetActiveTantouEditorContributorsAsync` method |
| Application | `Application/DTOs/Editor/EditorDashboardDtos.cs` | Updated doc comment to reflect scoping |
| Application | `Application/DTOs/Manga/SeriesProposalDtos.cs` | Added `ProposalActiveEditorContributorDto`, added `ActiveTantouEditors` and `HasOtherActiveTantouEditor` to `EditorProposalDetailDto` |
| Application | `Application/Features/Editor/SeriesProposals/Queries/GetEditorProposalDetail/GetEditorProposalDetailQueryHandler.cs` | Queries active Tantou Editor contributors and populates new DTO fields |
| Infrastructure | `Infrastructure/Repositories/EditorDashboardRepository.cs` | Scoped Chapters Under Review and Pending Annotations to active contributor series IDs |
| Infrastructure | `Infrastructure/Repositories/SeriesProposalRepository.cs` | Implemented `GetActiveTantouEditorContributorsAsync` |
| Web | `Web/Components/Pages/Editor/ProposalReviewDetail.razor` | Added contextual claim UI (3 states), confirmation dialog for additional editor |

## Architecture flow
```
EditorDashboard.razor
  -> IEditorDashboardApiClient.GetDashboardAsync()
  -> GET /api/editor/dashboard
  -> EditorDashboardController
  -> IMediator.Send(GetEditorDashboardQuery)
  -> GetEditorDashboardQueryHandler
  -> IEditorDashboardRepository.GetDashboardDataAsync()
  -> EF Core AsNoTracking → SQL Server

ProposalReviewDetail.razor
  -> IEditorProposalApiClient.GetDetailAsync()
  -> GET /api/editor/proposals/{id}
  -> EditorProposalsController
  -> IMediator.Send(GetEditorProposalDetailQuery)
  -> GetEditorProposalDetailQueryHandler
  -> ISeriesProposalRepository.GetByIdWithDetailsAsync() + GetActiveTantouEditorContributorsAsync()
  -> EF Core AsNoTracking → SQL Server
```

## DB/SP impact
None. No schema changes, no stored procedure changes. Claim SP already supports multiple editors.

## Build Result
```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
```
- Build succeeded
- 0 errors
- 49 warnings (all pre-existing; no new warnings from changed files)

## Manual Test Checklist
- [ ] Log in as Editor1.
- [ ] Open `/editor`.
- [ ] Confirm Chapters Under Review only counts Editor1 active contributed series.
- [ ] Confirm Pending Annotations only counts Editor1 active contributed series.
- [ ] Confirm Serialized Series only counts Editor1 active contributed series.
- [ ] Confirm Pending Proposals shows the global claimable queue.
- [ ] Log in as Editor3.
- [ ] Open `/editor`.
- [ ] Confirm counts differ according to Editor3 active contributed series.
- [ ] Open proposal detail for a series with no active editor.
- [ ] Confirm normal `Unclaimed Proposal` claim UI appears.
- [ ] Claim proposal as Editor3.
- [ ] Log in as Editor1.
- [ ] Open same proposal detail.
- [ ] Confirm it does NOT show `Unclaimed Proposal`.
- [ ] Confirm it shows existing active editor(s) with "Active editor already assigned".
- [ ] Click "Claim as Additional Editor".
- [ ] Confirm warning dialog appears.
- [ ] Click Cancel and confirm dialog closes without action.
- [ ] Click "Claim as Additional Editor" again, then "Claim Anyway".
- [ ] Confirm claim succeeds and Editor1 has decision actions.
- [ ] Confirm multiple active editors are visible.
- [ ] Try claim as same editor already active and confirm duplicate is prevented.

## Known issues
None.

## Follow-ups
None.
