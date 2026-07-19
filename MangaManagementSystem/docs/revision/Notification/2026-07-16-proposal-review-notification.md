# PROPOSAL_REVIEW notification

## Date
2026-07-16

## Requirement
When a Mangaka submits a proposal, notify active Tantou Editor contributors of the exact series. Unrelated Editors must not receive it. Bell navigation must open the submitted proposal.

## Implementation
- Added `PROPOSAL_REVIEW` constant.
- Updated `SubmitSeriesProposalCommandHandler`.
- Reused `GetActiveTantouEditorContributorsAsync`.
- Stored procedure and EF notifications use one shared transaction.
- Bound the stored-procedure command with `GetDbTransaction()`.
- Added Bell route `/editor/proposals/{seriesProposalId}`.
- No schema or stored-procedure definition changed.

## Validation
- Release build: 68 warnings, 0 errors.
- User: `TestMangaka1`
- Series: `PR82 Proposal Decision Runtime Test`
- Proposal: `v2`
- Proposal ID: `20C9F259-1F24-4F36-A3BE-E04D6749627C`
- Status: `UNDER_EDITORIAL_REVIEW`
- `TestEditor1` received one `PROPOSAL_REVIEW`.
- `TestEditor2` to `TestEditor5` received zero.
- Bell opened the exact proposal and marked the notification read.
- total_notification_count = 1
- distinct_recipient_count = 1
- read_notification_count = 1

## Result
`PROPOSAL_REVIEW` implementation and runtime validation: PASS.
