## Goal

Implement board poll, board vote, and board result workflow.

## Source IDs

Business Rules:
- BR-BOARD-POLL-001 to BR-BOARD-POLL-017
- BR-BOARD-VOTE-001 to BR-BOARD-VOTE-011
- BR-BOARD-RESULT-001 to BR-BOARD-RESULT-017

Functional Requirements:
- FR-BOARD-POLL-001 to FR-BOARD-POLL-018
- FR-BOARD-VOTE-001 to FR-BOARD-VOTE-012
- FR-BOARD-RESULT-001 to FR-BOARD-RESULT-016

User Stories:
- US-BOARD-001 to US-BOARD-008
- US-ADMIN-004
- US-ADMIN-005
- US-ADMIN-006
- US-ADMIN-007
- US-SYSADMIN-005

## Description

Admins manage board polls. Editorial Board Members vote. Board results are computed from votes and applied only when the poll is closed. There is no separate SeriesBoardDecision table in MVP.

## Acceptance Criteria

- [ ] Admin can open START_SERIALIZATION and CANCEL_SERIALIZATION polls.
- [ ] Board members can vote only in open polls.
- [ ] Each board member can vote at most once per poll.
- [ ] Reject vote requires reason.
- [ ] Approve > reject computes APPROVED.
- [ ] Reject > approve computes REJECTED.
- [ ] Tie computes NO_DECISION.
- [ ] Closed poll result can update series/proposal status.
- [ ] Cancelled poll preserves votes but does not affect status.
- [ ] Result application is audit-log ready.

## Out of Scope

- Separate SeriesBoardDecision table.
