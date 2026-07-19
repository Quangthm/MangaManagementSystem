# PR #82 — Proposal and Chapter Decision Notifications

## Date

2026-07-16

## Branch

`feature/notification-remaining-flows`

## Scope

Implemented and runtime validated:

- `PROPOSAL_DECISION`
- `CHAPTER_DECISION`

No database schema, migration, CHECK constraint, trigger, or stored procedure was added or modified.

## PROPOSAL_DECISION

Notification is created when a Tantou Editor records an editorial proposal decision:

- Request Revision
- Pass To Board
- Cancel Proposal

Recipients are active Mangaka contributors of the affected series.

Runtime case:

- Series: `PR82 Proposal Decision Runtime Test`
- Proposal version: `v1`
- Submitter and recipient: `TestMangaka1`
- Reviewer: `TestEditor1`
- Decision: `REVISION_REQUESTED`
- Review comment:
  `PR82 runtime validation - proposal decision notification.`
- Notification title: `Proposal Revision Requested`
- Related entity type: `SeriesProposal`
- Bell route: `/mangaka/proposals/{proposalId}`
- UI runtime result: PASS
- Database notification count: 1
- `read_at_utc`: populated after Bell navigation
- Duplicate result: none

The proposal stored-procedure transition and EF notification insertion use the shared Unit of Work transaction.

## CHAPTER_DECISION

Notification is created when a Tantou Editor records a chapter review decision:

- Approved
- Revision Requested
- Cancelled

Recipients are active Mangaka contributors of the affected series.

Runtime case:

- Series: `Notification Frequency Runtime Test`
- Chapter: `2 — Chapter Review Runtime Test`
- Creator and recipient: `TestMangaka1`
- Reviewer: `TestEditor1`
- Decision: `REVISION_REQUESTED`
- Review comment:
  `PR82 runtime validation - chapter decision notification.`
- Notification title: `Chapter Revision Requested`
- Related entity type: `Chapter`
- Bell route: `/mangaka/chapters`
- UI runtime result: PASS
- Database notification count: 1
- `read_at_utc`: populated after Bell navigation
- Duplicate result: none

The chapter decision, Audit Event, and notification are committed through the existing shared EF transaction.

## Build

Release build completed successfully:

- Errors: 0
- Warnings: 69 pre-existing warnings outside the changed notification files

## Files changed

- `NotificationConstants.cs`
- `ProposalDecisionNotificationSupport.cs`
- `RequestProposalRevisionCommandHandler.cs`
- `PassProposalToBoardCommandHandler.cs`
- `CancelProposalReviewCommandHandler.cs`
- `EditorChapterReviewRepository.Scheduling.cs`
- `NotificationBell.razor`

## Final status

`PROPOSAL_DECISION` and `CHAPTER_DECISION` are implemented and runtime validated through UI and database evidence.