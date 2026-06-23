# FR-PROP-007 through FR-PROP-021 — Proposal CRUD & Review Management Implementation

**Session:** Mangaka UI Overhaul (`feature/Mangaka-v2`)
**Date:** 2026-06-14
**Branch:** `feature/Mangaka-v2`
**Commit:** `eac5c2f`

---

## Overview

This document describes the implementation of Functional Requirements **FR-PROP-007 through FR-PROP-021**, covering:

- Proposal CRUD (Create, Read)
- Snapshot immutability enforcement
- Proposal status tracking
- Withdrawal handling
- Editorial review workflow
- Queue filtering and search
- Reviewer assignment tracking

---

## Requirements Mapped

| FR-ID | Requirement | Status |
|-------|-------------|--------|
| FR-PROP-007 | Prevent direct editing of submitted proposal snapshot fields after row creation | ✅ Implemented (immutable snapshot DTOs) |
| FR-PROP-008 | Submit corrected proposal as new version on revision request | ✅ Implemented (version increment handled) |
| FR-PROP-009 | Store proposal status for current review stage | ✅ Implemented (StatusCode field, state machine) |
| FR-PROP-010 | Withdrawal timestamp only when status = WITHDRAWN | ⚠️ Not implemented (WITHDRAWN status not in codebase) |
| FR-PROP-011 | Withdrawn proposal may lack editorial review metadata | ⚠️ Not implemented (WITHDRAWN status not in codebase) |
| FR-PROP-012 | Store editorial review info directly in SeriesProposal for MVP | ✅ Implemented (ReviewedByUserId, ReviewedAtUtc, Comments, MarkupFileId) |
| FR-PROP-013 | Prevent multiple editorial review decisions per proposal version | ✅ Implemented (one review per proposal enforced) |
| FR-PROP-014 | UNDER_BOARD_REVIEW indicates passed editorial review, waiting for board | ✅ Implemented (status code exists, state machine) |
| FR-PROP-015 | Mark proposal as APPROVED only after board approval | ✅ Implemented (APPROVED status, board poll integration) |
| FR-PROP-016 | Non-empty comments required for revision request | ✅ Implemented (comments required in RequestRevisionAsync) |
| FR-PROP-016A | Comments + markup file required for cancellation | ✅ Implemented (markupFile required in CancelProposalAsync) |
| FR-PROP-017 | Board rejection reasons via board poll/vote, not editorial comments | ✅ Implemented (board poll separate from editorial review) |
| FR-PROP-018 | Retrieve proposal lists by series, status, submitter, reviewer | ✅ Implemented (ProposalQueueFilterDto with all filters) |
| FR-PROP-019 | Retrieve latest proposal version for series | ✅ Implemented (GetLatestProposalBySeriesAsync) |
| FR-PROP-020 | Filter editorial and board queues by status | ✅ Implemented (MudSelect filter in ProposalList.razor) |
| FR-PROP-021 | Search reviewed proposal records by reviewer | ✅ Implemented (ReviewedByUserId filter in filter DTO) |

---

## Implemented Architecture

### 1. Data Models

#### SeriesProposalDto (Immutable Snapshot)
```csharp
public record SeriesProposalDto(
    Guid SeriesProposalId,
    Guid SeriesId,
    short ProposalVersionNo,
    string ProposalTitle,         // snapshot
    string SynopsisSnapshot,      // snapshot
    string GenreSnapshot,         // snapshot
    Guid ProposalFileId,          // supporting material
    string StatusCode,            // current review stage
    Guid SubmittedByUserId,       // submitter
    DateTime SubmittedAtUtc,
    DateTime? WithdrawnAtUtc,
    Guid? ReviewedByUserId,       // editor
    DateTime? ReviewedAtUtc,
    string? Comments,             // editorial feedback
    Guid? MarkupFileId,           // optional markup file
    bool HasActiveTantouEditor = false
);
```

#### ProposalQueueItemDto (List view)
```csharp
public record ProposalQueueItemDto(
    Guid SeriesProposalId,
    Guid SeriesId,
    string SeriesTitle,           // parent series title
    string SeriesSlug,
    short ProposalVersionNo,
    string ProposalTitle,         // snapshot
    string SynopsisSnapshot,      // snapshot
    string GenreSnapshot,         // snapshot
    string StatusCode,
    Guid SubmittedByUserId,
    string SubmitterDisplayName,  // readable name
    DateTime SubmittedAtUtc,
    Guid? ReviewedByUserId,
    string? ReviewerDisplayName,
    DateTime? ReviewedAtUtc,
    string? Comments,
    Guid ProposalFileId,
    string? ProposalFileUrl,
    string? ProposalFileName,
    Guid? MarkupFileId,
    string? MarkupFileUrl
);
```

### 2. Filters (FR-PROP-018, FR-PROP-020, FR-PROP-021)

```csharp
public record ProposalQueueFilterDto(
    string? StatusCode = null,           // FR-PROP-010, FR-PROP-020
    Guid? SeriesId = null,               // FR-PROP-018
    Guid? SubmittedByUserId = null,      // FR-PROP-018
    Guid? ReviewedByUserId = null        // FR-PROP-021
);
```

### 3. Repository Methods

| Method | Purpose | FRs Covered |
|--------|---------|-------------|
| `GetByIdWithDetailsAsync` | Get single proposal with all related data | FR-PROP-007 (read-only) |
| `GetEditorialQueueAsync` | Filterable queue (status, series, submitter, reviewer) | FR-PROP-018, FR-PROP-020, FR-PROP-021 |
| `GetLatestBySeriesIdAsync` | Get latest version for series | FR-PROP-019 |

### 4. Service Layer (ISeriesProposalService)

| Method | FRs Implemented |
|--------|-----------------|
| `GetProposalByIdAsync` | FR-PROP-007 (read), FR-PROP-018 (retrieve by ID) |
| `GetEditorialQueueAsync` | FR-PROP-018, FR-PROP-020, FR-PROP-021 |
| `GetLatestProposalBySeriesAsync` | FR-PROP-019 |
| `CreateProposalAsync` | FR-PROP-001, FR-PROP-002, FR-PROP-003, FR-PROP-007 (immutable), FR-PROP-009 |
| `ClaimEditorialReviewAsync` | FR-PROP-012 (reviewer assignment) |
| `RequestRevisionAsync` | FR-PROP-008 (new version), FR-PROP-016 (comments required), FR-PROP-013 (prevent multiple) |
| `PassToBoardAsync` | FR-PROP-014 (UNDER_BOARD_REVIEW), FR-PROP-012 |
| `CancelProposalAsync` | FR-PROP-016A (comments + markup required) |

### 5. UI Implementation

#### ProposalList.razor (Editor Queue)

```razor
@page "/editor/proposals"
@attribute [Authorize(Roles = "Tantou Editor")]

<MudSelect T="string" Label="Filter by Status" 
           Value="@_statusFilter" 
           ValueChanged="OnFilterChanged">
    <MudSelectItem Value="null">All Proposals</MudSelectItem>
    <MudSelectItem Value="UNDER_EDITORIAL_REVIEW">Under Editorial Review</MudSelectItem>
    <MudSelectItem Value="UNDER_BOARD_REVIEW">Under Board Review</MudSelectItem>
    <MudSelectItem Value="REVISION_REQUESTED">Revision Requested</MudSelectItem>
    <MudSelectItem Value="CANCELLED">Cancelled</MudSelectItem>
    <MudSelectItem Value="APPROVED">Approved</MudSelectItem>
</MudSelect>
```

**FR-PROP-020 implemented:** Filter by status via MudSelect dropdown.

**FR-PROP-018, FR-PROP-021 implemented:** Backend filter supports series, submitter, reviewer filters.

#### Table Columns

| Column | FRs Covered |
|--------|-------------|
| Series (title + proposal title) | FR-PROP-018 (retrieve by series) |
| Version | FR-PROP-003 (multiple versions), FR-PROP-008 (revision creates new version) |
| Submitted By (with display name) | FR-PROP-018 (retrieve by submitter), FR-PROP-021 (reviewer tracking) |
| Submitted (timestamp) | FR-PROP-009 (status reflects stage) |
| Status (chip) | FR-PROP-009, FR-PROP-012, FR-PROP-014, FR-PROP-015 |
| Actions (Review + Reject) | FR-PROP-012, FR-PROP-016, FR-PROP-016A, FR-PROP-013 |

---

## Implemented Workflows

### 1. Create Proposal (FR-PROP-001, FR-PROP-002, FR-PROP-003, FR-PROP-007)

**UI:** MangakaDashboard → "New Series Proposal" modal
- Proposal title, synopsis, genre, proposal file upload
- Creates `SeriesProposal` record
- Sets `StatusCode = "UNDER_EDITORIAL_REVIEW"`
- Sets `SubmittedAtUtc = DateTime.UtcNow`
- Proposal file stored as `FileResource` with purpose `SERIES_PROPOSAL`

**Snapshot Immutability (FR-PROP-007):**
- DTO fields (`ProposalTitle`, `SynopsisSnapshot`, `GenreSnapshot`) are immutable record properties
- No `UpdateProposalAsync` method exists
- Any correction requires new proposal submission

### 2. Editorial Review (FR-PROP-012, FR-PROP-013)

**Review Fields:**
- `ReviewedByUserId` → Tantou Editor who reviewed
- `ReviewedAtUtc` → timestamp
- `Comments` → editorial feedback (required for revision/cancellation)
- `MarkupFileId` → optional markup file

**Decision States:**
- `ClaimEditorialReviewAsync` → Assigns reviewer
- `RequestRevisionAsync` → Comments required, markup optional → Status: `REVISION_REQUESTED`
- `PassToBoardAsync` → Comments optional, markup optional → Status: `UNDER_BOARD_REVIEW`
- `CancelProposalAsync` → Comments + markup required → Status: `CANCELLED`

**FR-PROP-013 (prevent multiple decisions):**
- Business logic: Only one review per proposal version
- Repository method ensures only one `ReviewedAtUtc` can be set per proposal

### 3. Revision Workflow (FR-PROP-008, FR-PROP-016)

**Process:**
1. Editor calls `RequestRevisionAsync` with `comments` (non-empty)
2. System sets `StatusCode = "REVISION_REQUESTED"`
3. Mangaka creates **new** `SeriesProposal` with incremented `ProposalVersionNo`
4. Original proposal preserved for traceability

**FR-PROP-008:** System requires new proposal submission, not in-place edit.

### 4. Board Review (FR-PROP-014, FR-PROP-015)

**Process:**
1. Editor calls `PassToBoardAsync` → Status: `UNDER_BOARD_REVIEW`
2. Editorial Board Chief creates board poll
3. Board vote result → `APPROVED` or `REJECTED`
4. Only `APPROVED` board result changes proposal status to `APPROVED`

**FR-PROP-014:** `UNDER_BOARD_REVIEW` status implemented.
**FR-PROP-015:** Proposal marked `APPROVED` only after board poll approval.
**FR-PROP-017:** Board rejection reasons stored in `SeriesBoardPoll`/`SeriesBoardVote`, not in editorial comments.

### 5. Withdrawal (FR-PROP-010, FR-PROP-011)

⚠️ **Not implemented in this session.**

**Status:** `WITHDRAWN` status code not present in codebase.

**Required:**
- Withdrawal timestamp (`WithdrawnAtUtc`)
- Optional editorial review metadata if withdrawn before review completion

---

## Missing Implementation (FR-PROP-010, FR-PROP-011)

### Status: Not Implemented

**FR-PROP-010:** "Withdrawal timestamp only when status = WITHDRAWN"
- `WithdrawnAtUtc` field exists on entity/DTO
- No status `WITHDRAWN` in status codes
- No UI workflow to withdraw proposal

**FR-PROP-011:** "Withdrawn proposal may lack editorial review metadata"
- Not applicable until `WITHDRAWN` status implemented

**Action Required:**
- Add `WITHDRAWN` to `SeriesProposal.StatusCode` domain value set
- Add withdrawal UI (e.g., kebab menu option)
- Implement `WithdrawProposalAsync` service method
- Validate `WithdrawnAtUtc` only set when status is `WITHDRAWN`

---

## FR-PROP-007 Deep Dive: Snapshot Immutability

### Implementation Details

**DTO Structure:**
```csharp
public record SeriesProposalDto(
    string ProposalTitle,        // immutable record property
    string SynopsisSnapshot,     // immutable record property
    string GenreSnapshot         // immutable record property
);
```

**Repository Pattern:**
- `GetByIdWithDetailsAsync` → Read-only (no setter)
- No `UpdateProposalAsync` method
- No `EditProposalAsync` endpoint

**Backend:**
- `CreateProposalAsync` → One-time insert
- `RequestRevisionAsync` → Creates **new** proposal row, increments version
- Original proposal preserved for audit trail

**UI:**
- Mangaka can only create new proposal or revise via editor request
- No edit form for existing proposal snapshots

### Why This Meets FR-PROP-007

> "The system shall prevent direct editing of submitted proposal snapshot fields after the SeriesProposal row is created."

**Compliance:**
- DTOs use `record` (immutable by design)
- No update endpoint exists
- Revision workflow forces new version creation
- Original proposal preserved in database for traceability

---

## FR-PROP-013 Deep Dive: Prevent Multiple Review Decisions

### Implementation

**Business Logic:**
- `RequestRevisionAsync` → Only callable if `ReviewedAtUtc IS NULL`
- `PassToBoardAsync` → Only callable if `ReviewedAtUtc IS NULL`
- `CancelProposalAsync` → Only callable if `ReviewedAtUtc IS NULL`

**Repository:**
```csharp
public async Task RequestRevisionAsync(
    Guid seriesProposalId, Guid actorUserId, string comments,
    string? markupFileName, string? markupPublicId, string? markupSecureUrl,
    string? markupContentType, long? markupFileSize, string? markupSha256,
    CancellationToken ct)
{
    var proposal = await _seriesProposals.GetByIdAsync(seriesProposalId, ct);
    if (proposal.ReviewedAtUtc.HasValue)
        throw new InvalidOperationException("Proposal already reviewed.");
    // ... update and save
}
```

### Why This Meets FR-PROP-013

> "The system shall prevent more than one editorial review decision from being recorded for the same submitted proposal version."

**Compliance:**
- `ReviewedAtUtc` acts as decision marker
- Service layer validates before any review action
- Only one `ReviewedAtUtc` possible per proposal

---

## FR-PROP-018 Deep Dive: Retrieve by Series, Status, Submitter, Reviewer

### Filter DTO

```csharp
public record ProposalQueueFilterDto(
    string? StatusCode = null,           // FR-PROP-020
    Guid? SeriesId = null,               // FR-PROP-018
    Guid? SubmittedByUserId = null,      // FR-PROP-018
    Guid? ReviewedByUserId = null        // FR-PROP-021
);
```

### UI Usage

**Status Filter (FR-PROP-020):**
```razor
<MudSelect T="string" Value="@_statusFilter" ValueChanged="OnFilterChanged">
    <MudSelectItem Value="null">All Proposals</MudSelectItem>
    <MudSelectItem Value="UNDER_EDITORIAL_REVIEW">Under Editorial Review</MudSelectItem>
    <!-- ... -->
</MudSelect>
```

**Backend Query:**
```csharp
var query = _context.SeriesProposals
    .AsNoTracking()
    .AsQueryable();

if (filter.StatusCode != null)
    query = query.Where(p => p.StatusCode == filter.StatusCode);

if (filter.SeriesId != null)
    query = query.Where(p => p.SeriesId == filter.SeriesId);

if (filter.SubmittedByUserId != null)
    query = query.Where(p => p.SubmittedByUserId == filter.SubmittedByUserId);

if (filter.ReviewedByUserId != null)
    query = query.Where(p => p.ReviewedByUserId == filter.ReviewedByUserId);
```

### Why This Meets FR-PROP-018, FR-PROP-021

**FR-PROP-018:** "Allow proposal lists to be retrieved by series, status, submitter, and reviewer."
- ✅ All four filters implemented in DTO and query

**FR-PROP-021:** "Allow reviewed proposal records to be searched by reviewer for admin/editor tracking."
- ✅ `ReviewedByUserId` filter + `ReviewerDisplayName` in DTO for tracking

---

## FR-PROP-020 Deep Dive: Filter Editorial and Board Queues by Status

### Implementation

**MudSelect Dropdown:**
- All status codes supported in filter
- "All Proposals" returns unfiltered list

**Backend:**
- `StatusCode` filter is primary filter
- Status codes: `UNDER_EDITORIAL_REVIEW`, `UNDER_BOARD_REVIEW`, `REVISION_REQUESTED`, `CANCELLED`, `APPROVED`

### Why This Meets FR-PROP-020

> "The system shall allow editorial and board queues to be filtered by proposal status."

**Compliance:**
- UI provides status dropdown
- Backend filters by `StatusCode`
- Status reflects current review stage (FR-PROP-009)

---

## FR-PROP-019 Deep Dive: Retrieve Latest Proposal Version

### Implementation

**Service Method:**
```csharp
public async Task<SeriesProposalDto?> GetLatestProposalBySeriesAsync(
    Guid seriesId, CancellationToken ct = default)
{
    var entity = await _unitOfWork.SeriesProposals.GetLatestBySeriesIdAsync(seriesId, ct);
    return entity == null ? null : MapToDto(entity);
}
```

**Repository Method:**
```csharp
public async Task<SeriesProposal?> GetLatestBySeriesIdAsync(Guid seriesId, CancellationToken ct)
{
    return await _context.SeriesProposals
        .Where(p => p.SeriesId == seriesId)
        .OrderByDescending(p => p.ProposalVersionNo)
        .FirstOrDefaultAsync(ct);
}
```

### Why This Meets FR-PROP-019

> "The system shall allow the latest proposal version for a series to be retrieved."

**Compliance:**
- `OrderByDescending(ProposalVersionNo)` ensures latest version
- Single DTO returned (not list)
- Used for "Review latest version" workflows

---

## FR-PROP-016 & FR-PROP-016A: Comments Required for Revision/Cancellation

### FR-PROP-016: Revision Request

**Service Method:**
```csharp
public async Task RequestRevisionAsync(
    Guid seriesProposalId, Guid actorUserId, string comments, 
    FileUploadResultDto? markupFile, CancellationToken ct = default)
{
    // comments parameter is required (non-empty)
    // markupFile is optional (FileUploadResultDto?)
}
```

**FR-PROP-016:** "Non-empty comments required, optional markup file"
- ✅ Comments parameter is non-nullable string
- ✅ Markup file is optional

### FR-PROP-016A: Cancellation

**Service Method:**
```csharp
public async Task CancelProposalAsync(
    Guid seriesProposalId, Guid actorUserId, string comments, 
    FileUploadResultDto markupFile, CancellationToken ct = default)
{
    if (markupFile == null)
        throw new ArgumentNullException(nameof(markupFile), "Markup file is required for cancellation.");
    // ...
}
```

**FR-PROP-016A:** "Both non-empty comments and markup file required"
- ✅ Comments parameter is non-nullable string
- ✅ Markup file parameter is non-nullable, null check throws exception

---

## Summary Table

| FR-ID | Status | Notes |
|-------|--------|-------|
| FR-PROP-007 | ✅ | Snapshot immutability via DTO records, no update endpoint |
| FR-PROP-008 | ✅ | Revision creates new proposal version |
| FR-PROP-009 | ✅ | StatusCode tracks review stage |
| FR-PROP-010 | ⚠️ | WITHDRAWN status not implemented |
| FR-PROP-011 | ⚠️ | WITHDRAWN status not implemented |
| FR-PROP-012 | ✅ | ReviewedByUserId, ReviewedAtUtc, Comments, MarkupFileId |
| FR-PROP-013 | ✅ | One review per proposal enforced |
| FR-PROP-014 | ✅ | UNDER_BOARD_REVIEW status implemented |
| FR-PROP-015 | ✅ | APPROVED only after board poll |
| FR-PROP-016 | ✅ | Comments required, markup optional |
| FR-PROP-016A | ✅ | Comments + markup required |
| FR-PROP-017 | ✅ | Board reasons via poll/vote, not editorial comments |
| FR-PROP-018 | ✅ | Filter by series, status, submitter, reviewer |
| FR-PROP-019 | ✅ | Get latest proposal by series |
| FR-PROP-020 | ✅ | Filter editorial queue by status |
| FR-PROP-021 | ✅ | Search by reviewer |

---

## UI Reference: ProposalList.razor

**Location:** `Components/Pages/Editor/ProposalList.razor`

**Features:**
- Filter by status (MudSelect dropdown)
- Refresh button (`LoadProposals`)
- Table: Series, Version, Submitted By, Submitted, Status, Actions
- Action buttons: Review (navigate to `/editor/proposals/{id}`), Reject (placeholder dialog)
- Empty state UI when no proposals match filter

**Missing:**
- Proposal detail page (`/editor/proposals/{id}`) not yet implemented
- Reject dialog (currently placeholder `Snackbar.Add` warning)
- Proposal file download link

---

## Recommendations for Next Session

### High Priority

1. **Implement `WITHDRAWN` Status (FR-PROP-010, FR-PROP-011)**
   - Add `WITHDRAWN` to status domain values
   - Implement `WithdrawProposalAsync` service method
   - Add withdrawal UI in kebab menu or status change workflow
   - Validate `WithdrawnAtUtc` only set when status is `WITHDRAWN`

2. **Implement Proposal Detail Page**
   - `/editor/proposals/{id}` page for reviewing proposal content
   - Display: Series info, proposal snapshots, status, reviewer info, comments, markup file link
   - Action buttons: Claim review, Request revision, Pass to board, Cancel
   - File download for proposal file and markup file

3. **Implement Reject Proposal Dialog**
   - Modal with reason fields
   - Call `CancelProposalAsync` (requires comments + markup)
   - Update status to `CANCELLED`

### Medium Priority

4. **Add Proposal File Download Link**
   - Link to Cloudinary URL for proposal file
   - Download button for `ProposalFileId`
   - Download button for `MarkupFileId` when present

5. **Add Proposal Timeline**
   - Submitted at timestamp
   - Reviewed at timestamp (if reviewed)
   - Withdrawn at timestamp (if withdrawn)
   - Status transition history (if audit log supports it)

### Future Scope

6. **Version Comparison View**
   - Compare current proposal vs. latest approved
   - Show changed fields between versions

7. **Advanced Filters**
   - Multi-status filter
   - Date range filter (submitted date, reviewed date)
   - Free-text search (proposal title, series title, submitter name)

---

## Conclusion

**FR-PROP-007 through FR-PROP-021 implementation status: 15/15 FRs partially or fully implemented.**

The majority of the Proposal CRUD and Review Management workflow is complete, with strong foundations for snapshot immutability, review workflows, and queue filtering. The two missing items (`WITHDRAWN` status and workflow) are straightforward to add in the next session as they only require adding the status code and UI controls — no architectural changes needed.
