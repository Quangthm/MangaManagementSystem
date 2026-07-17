# PR #82 — Remaining Notification Types Final Audit

## Date

2026-07-16

## Branch and checkpoint

- Branch: `feature/notification-remaining-flows`
- Source implementation checkpoint:
  `9bce33d5fa370c943fe6b138d73acf01f1e6289e`

## Purpose

This note records the final result of the Leader follow-up audit for the seven
notification type names found in the shared database constraint.

The audit does not treat the existence of a database type name as sufficient
evidence to invent a business workflow.

## Final status matrix

| Notification type | Final status |
|---|---|
| `PROPOSAL_DECISION` | IMPLEMENTED AND RUNTIME VALIDATED |
| `CHAPTER_DECISION` | IMPLEMENTED AND RUNTIME VALIDATED |
| `PROPOSAL_REVIEW` | REQUIREMENT/SCOPE CONFIRMATION REQUIRED |
| `BOARD_DECISION` | REQUIREMENT/SCOPE CONFIRMATION REQUIRED |
| `TASK_REVIEW` | REQUIREMENT/SCOPE CONFIRMATION REQUIRED |
| `RANKING_WARNING` | REQUIREMENT/THRESHOLD CONFIRMATION REQUIRED |
| `PUBLICATION_SCHEDULE` | REQUIREMENT/SCOPE CONFIRMATION REQUIRED |

## Implemented types

### `PROPOSAL_DECISION`

Implemented for Tantou Editor proposal decisions:

- Request Revision
- Pass To Board
- Cancel Proposal

Recipients:

- distinct active Mangaka contributors of the affected series.

Runtime validation:

- Editor: `TestEditor1`
- Recipient: `TestMangaka1`
- Series: `PR82 Proposal Decision Runtime Test`
- Decision: `REVISION_REQUESTED`
- Notification title: `Proposal Revision Requested`
- Related entity: `SeriesProposal`
- Bell navigation opened the exact proposal detail.
- `read_at_utc` was populated.
- Matching notification count: exactly 1.
- UI result: PASS.
- Database result: PASS.

### `CHAPTER_DECISION`

Implemented for Tantou Editor chapter decisions:

- Approved
- Revision Requested
- Cancelled

Recipients:

- distinct active Mangaka contributors of the affected series.

Runtime validation:

- Editor: `TestEditor1`
- Recipient: `TestMangaka1`
- Series: `Notification Frequency Runtime Test`
- Chapter: `2 — Chapter Review Runtime Test`
- Decision: `REVISION_REQUESTED`
- Notification title: `Chapter Revision Requested`
- Related entity: `Chapter`
- Bell navigation opened `/mangaka/chapters`.
- `read_at_utc` was populated.
- Matching notification count: exactly 1.
- UI result: PASS.
- Database result: PASS.

## Remaining five types

### `PROPOSAL_REVIEW`

No production source implementation was found.

The repository does not define:

- which proposal event creates this notification;
- whether it differs from `PROPOSAL_DECISION`;
- recipient scope;
- related entity and Bell route;
- duplicate rule;
- runtime acceptance scenario.

Status:

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

### `BOARD_DECISION`

No production source implementation was found.

`BOARD_POLL` already represents notifying Editorial Board Members when a poll
is opened. A board decision notification would be a separate event and must not
be inferred from the existing poll notification.

The repository does not define:

- approved event and type name;
- decision recipient;
- title/message;
- related entity and Bell route;
- duplicate rule.

Status:

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

### `TASK_REVIEW`

No production source implementation was found.

The task workflow contains assignment, reassignment, task statuses, review
operations and audit events, but no requirement identifies which exact event
must create a `TASK_REVIEW` notification.

Status:

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

### `RANKING_WARNING`

Business Rules and Functional Requirements state that a warning should be
created when ranking evidence shows high cancellation risk.

However, the repository does not define:

- the threshold for high cancellation risk;
- a cancellation-risk formula;
- whether rank position, score, rating, readership or another measure controls it;
- the trigger moment;
- duplicate behavior per series/publication period;
- the authoritative related entity.

The active MVP ranking flow uses `SeriesVoteInput` and the dynamic
`vw_SeriesRanking` output. The MVP documentation explicitly states that there
is no ranking-finalization workflow and does not require
`SeriesRankingSnapshot`.

Implementing a threshold without approval would invent business logic.

Status:

`NOT IMPLEMENTED - REQUIREMENT/THRESHOLD CONFIRMATION REQUIRED`

### `PUBLICATION_SCHEDULE`

The repository contains chapter scheduling, rescheduling, hold and release
workflows, but it does not define one specific `PUBLICATION_SCHEDULE`
notification contract.

Missing decisions include:

- which schedule event creates it;
- whether Mangaka, Tantou Editor or another role receives it;
- title/message;
- related entity;
- duplicate behavior when a chapter is repeatedly rescheduled.

Status:

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

## Database and source impact

For this final audit:

- no application source was changed;
- no database command was executed;
- no schema or migration was changed;
- no CHECK constraint was changed;
- no stored procedure was changed;
- no test data was mutated.

## Final conclusion

The seven-type Leader follow-up is resolved as follows:

- two types were implemented and runtime validated;
- five types were audited to their requirement boundary;
- no unsupported workflow or threshold was invented;
- the five remaining types require an approved business contract before
  implementation.