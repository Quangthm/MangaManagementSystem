# Series Proposal Tracking UI (Read-Only)

**Branch:** `feature/Mangaka`
**Date:** 2026-06-18

## Goal

Add a read-only Series Proposals tracking section to the Mangaka dashboard, allowing Mangaka users to view submitted proposals for their series, filter/search/sort, view proposal and markup files, and read review comments.

No mutation actions (edit, withdraw, approve, reject, upload markup) are included.

## Architecture Path

```
MangakaDashboard.razor
  -> IMangakaSeriesApiClient.GetMySeriesProposalsAsync(actorUserId)
  -> GET /api/mangaka/series/proposals (X-Actor-User-Id header)
  -> MangakaSeriesController.GetMySeriesProposalsAsync
  -> IMediator.Send(GetMySeriesProposalsQuery)
  -> GetMySeriesProposalsQueryHandler
  -> ISeriesProposalRepository.GetMySeriesProposalsAsync(actorUserId)
  -> EF Core read query (AsNoTracking + Include)
```

## Files Changed

### New files (2)

- `src/MangaManagementSystem.Application/Features/Mangaka/SeriesProposals/Queries/GetMySeriesProposals/GetMySeriesProposalsQuery.cs`
- `src/MangaManagementSystem.Application/Features/Mangaka/SeriesProposals/Queries/GetMySeriesProposals/GetMySeriesProposalsQueryHandler.cs`

### Modified files (7)

- `src/MangaManagementSystem.Application/DTOs/Manga/SeriesProposalDtos.cs` — added `MangakaSeriesProposalDto` and `ProposalFileRefDto`
- `src/MangaManagementSystem.Domain/Interfaces/ISeriesProposalRepository.cs` — added `GetMySeriesProposalsAsync`
- `src/MangaManagementSystem.Infrastructure/Repositories/SeriesProposalRepository.cs` — implemented `GetMySeriesProposalsAsync`
- `src/MangaManagementSystem.API/Controllers/MangakaSeriesController.cs` — added `GET proposals` endpoint
- `src/MangaManagementSystem.Web/Services/Api/IMangakaSeriesApiClient.cs` — added `GetMySeriesProposalsAsync`
- `src/MangaManagementSystem.Web/Services/Api/MangakaSeriesApiClient.cs` — implemented `GetMySeriesProposalsAsync`
- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor` — added "Series Proposals" nav item, proposals tab UI, detail modal

## API Endpoint Added

```
GET /api/mangaka/series/proposals
Header: X-Actor-User-Id: <actorUserId>
Response: IReadOnlyList<MangakaSeriesProposalDto>
```

Thin controller — resolves actor, dispatches MediatR query, returns result.

## Access Rule (Server-Side Scoped)

The EF query returns proposals only for series where the actor is an active Mangaka contributor:

```
SeriesProposal.SeriesId == SeriesContributor.SeriesId
AND SeriesContributor.UserId == actorUserId
AND SeriesContributor.EndDate IS NULL
AND SeriesContributor.User.StatusCode == "ACTIVE"
AND SeriesContributor.User.Role.RoleName == "Mangaka"
```

This matches the existing `GetByActiveContributorWithCoverAsync` scope pattern in `SeriesRepository`. Proposals are not filtered by `SubmittedByUserId` alone — contributor membership is the authoritative scope.

## Proposal Tab UI Behavior

- New sidebar nav item: **"Series Proposals"** (icon: `Description`)
- Header subtitle: "Track submitted proposal versions and review progress"
- Tab content renders only when `_activeTab == "proposals"`
- Proposals loaded via `LoadProposalTrackingAsync()` in `OnInitializedAsync`

## Filtering / Search / Sort / Pagination

### Status filter chips
- All
- Under Editorial Review (`UNDER_EDITORIAL_REVIEW`)
- Under Board Review (`UNDER_BOARD_REVIEW`)
- Revision Requested (`REVISION_REQUESTED`)
- Approved (`APPROVED`)
- Cancelled (`CANCELLED`)
- Withdrawn (`WITHDRAWN`)

Filter counts are based on the full scoped proposal list (`_proposalData`), not the current page.

### Search
Searches across: proposal title, series title, genre snapshot (case-insensitive).

### Sort modes
- Recently Submitted (default) — `SubmittedAtUtc DESC`
- Reviewed Date — `ReviewedAtUtc DESC`
- Proposal Title A-Z — `ProposalTitle ASC`
- Status — `ProposalStatusLabel ASC`

### Pagination
- Page size: 8
- Flow: `_proposalData -> filter -> search -> sort -> AllFilteredProposals -> PagedProposals -> render`
- Page resets to 1 on filter/search/sort change
- Empty state uses `AllFilteredProposals.Count`

### Proposal table columns
Series | Proposal | Version | Status | Submitted | Markup | View arrow

## Detail Modal Behavior

- Read-only `MudOverlay + MudCard` modal
- Shows:
  - Proposal title + series title + version + status badge
  - Series slug link (opens in new tab)
  - Submitted by / at
  - Reviewed by / at (if available)
  - Withdrawn at (if status WITHDRAWN)
  - Genre snapshot
  - Synopsis snapshot (bounded scrollable box, `max-height: 220px; overflow-y: auto`)
  - Review comments (if available, yellow background box)
  - Proposal file actions
  - Markup file actions
- No editing controls, no mutation buttons

## File Open/Download Behavior

### Proposal file (always present)
- "Open Proposal File" button — `Href` to `SecureUrl`, `Target="_blank"`
- "Download Proposal File" button — same `Href` + `Target="_blank"`
- Shows original file name and size below

### Markup file
- If `MarkupFile != null`: same Open/Download buttons as proposal file
- If `MarkupFile == null`: "No markup file uploaded yet." (italic, muted)

Direct `CloudinarySecureUrl` links are used for MVP. A proper download proxy with forced `Content-Disposition: attachment` is documented as future work.

## DTO Design

```csharp
public sealed record MangakaSeriesProposalDto(
    Guid SeriesProposalId,
    Guid SeriesId,
    string SeriesSlug,
    string SeriesTitle,
    short ProposalVersionNo,
    string ProposalTitle,
    string SynopsisSnapshot,
    string GenreSnapshot,
    string StatusCode,
    DateTime SubmittedAtUtc,
    DateTime? WithdrawnAtUtc,
    DateTime? ReviewedAtUtc,
    string? Comments,
    string SubmittedByDisplayName,
    string? ReviewedByDisplayName,
    ProposalFileRefDto ProposalFile,
    ProposalFileRefDto? MarkupFile);

public sealed record ProposalFileRefDto(
    Guid FileResourceId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string SecureUrl);  // mapped from FileResource.CloudinarySecureUrl
```

## Build Result

```
Build succeeded.
    61 Warning(s)     <-- all pre-existing, none new from this feature
    0 Error(s)
```

## Manual Test Checklist

1. [ ] Navigate to `/mangaka` — verify "Series Proposals" nav item in sidebar.
2. [ ] Click "Series Proposals" — tab content loads.
3. [ ] Verify status filter chips show correct counts.
4. [ ] Click each filter chip — table narrows correctly.
5. [ ] Type in search box — filters by proposal title, series title, genre.
6. [ ] Change sort mode — table reorders.
7. [ ] Verify pagination appears when > 8 proposals.
8. [ ] Click a proposal row — detail modal opens.
9. [ ] Verify detail modal shows all read-only fields.
10. [ ] Verify series slug link opens in new tab.
11. [ ] Verify synopsis snapshot is in bounded scrollable box (max-height ~220px).
12. [ ] Verify review comments show in yellow box when present.
13. [ ] Verify "Open Proposal File" opens file in new tab.
14. [ ] Verify "Download Proposal File" opens file in new tab.
15. [ ] Verify markup file section shows "No markup file uploaded yet." when null.
16. [ ] Verify markup file Open/Download buttons when markup exists.
17. [ ] Verify withdrawn date shows for WITHDRAWN proposals.
18. [ ] Verify no edit/withdraw/approve/reject buttons exist.
19. [ ] Series with 0 proposals — shows empty state message.
20. [ ] Verify page resets to 1 on filter/search/sort change.

## Remaining Tasks

- Download proxy for forced attachment download (future improvement).
- Lazy loading proposals only when tab is first selected (optimization).
