> **Status update — 2026-07-16**
>
> This document was a point-in-time pre-implementation audit at commit
> `ef2ab3d13055419001e069f72baba36275ca8556`.
>
> The conclusions for `PROPOSAL_DECISION` and `CHAPTER_DECISION` are superseded:
> both types were subsequently implemented and runtime validated in commit
> `9bce33d5fa370c943fe6b138d73acf01f1e6289e`.
>
> The remaining five types continue to require approved requirement/scope
> confirmation as documented in:
> `2026-07-16-remaining-notification-types-final-audit.md`.
# PR #82 - Seven Notification Types Requirement and Scope Audit

Date: 2026-07-15

Branch: `feature/notification-remaining-flows`

Starting HEAD:

`e1febbe2fe5831cb880fb16b3ec07daa77aeeb98`

## Purpose

The Leader requested a careful audit of the following seven notification type
names:

1. `PROPOSAL_REVIEW`
2. `PROPOSAL_DECISION`
3. `BOARD_DECISION`
4. `TASK_REVIEW`
5. `CHAPTER_DECISION`
6. `RANKING_WARNING`
7. `PUBLICATION_SCHEDULE`

The audit must not treat the existence of a database type name, workflow page,
controller, repository, DTO or status code as sufficient authorization to
implement a Notification flow.

For each type, implementation requires confirmed answers for:

- exact business event;
- BR, FR or User Story traceability;
- triggering handler, service or repository;
- recipient role and participant scope;
- title and message;
- related entity type and ID;
- Notification Bell destination;
- transaction boundary;
- duplicate or idempotency rule;
- runtime test scenario.

## Audit method

The audit inspected:

- Notification constants;
- Notification Bell routing;
- API controllers;
- Application commands and handlers;
- services and repositories;
- Web pages and typed API clients;
- business-rule, functional-requirement and revision documentation;
- current PR #82 scope.

The audit was READ-ONLY.

It did not:

- change production source;
- add Notification constants;
- add Bell routes;
- modify the database;
- modify a CHECK constraint;
- modify a stored procedure;
- create test data;
- execute runtime mutations;
- commit or push.

## Shared conclusion

None of the seven names currently has enough approved requirement evidence to
define a complete Notification contract.

Some related workflows exist in source, but source existence does not answer
the Notification-specific questions of:

- when the Notification is created;
- who receives it;
- whether the recipient is global, role-based or participant-scoped;
- which entity the Bell opens;
- how duplicates are rejected;
- which writes must share a transaction.

Therefore, all seven types remain outside implementation scope until the
Leader confirms their exact contracts.

No source implementation or runtime PASS is claimed for these seven types.

---

## 1. `PROPOSAL_REVIEW`

### Source findings

The repository contains proposal-review workflow references, including:

- Tantou Editor proposal dashboard/read models;
- proposal-review queues;
- proposal-review detail UI;
- proposal status and reviewer information.

These references establish that proposal review exists as a business area.

They do not establish a Notification contract.

### Requirement findings

The audit found no approved BR, FR, User Story or Leader instruction defining:

- which proposal event triggers `PROPOSAL_REVIEW`;
- whether the trigger is proposal submission, claim, assignment or another
  review transition;
- whether recipients are all Tantou Editors or only eligible participants;
- whether series-contributor scoping applies;
- the title and message;
- the related entity;
- the Bell route;
- duplicate handling;
- transaction behavior.

### Implementation findings

No complete `PROPOSAL_REVIEW` vertical slice was established in PR #82.

The audit did not confirm:

- a production Notification creation point;
- a Notification plan;
- recipient-resolution code;
- a Bell route;
- runtime evidence.

### Leader confirmation required

The Leader must confirm:

1. The exact proposal event that creates the Notification.
2. Whether the recipient is:
   - all active Tantou Editors;
   - one claimed reviewer;
   - active Tantou contributors of the exact series;
   - another participant set.
3. Whether the related entity is:
   - `SeriesProposal`;
   - `Series`;
   - another entity.
4. The expected Bell destination.
5. The duplicate rule when a proposal is submitted, claimed or reopened more
   than once.
6. Whether proposal state, Audit Event and Notification must share one
   transaction.

### Status

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

---

## 2. `PROPOSAL_DECISION`

### Source findings

The repository contains proposal lifecycle and decision-related behavior,
including:

- proposal status transitions;
- reviewed and approved proposal state;
- editorial and board review workflows;
- series-navigation behavior derived from proposal approval state.

These references establish that proposal decisions exist.

They do not define a `PROPOSAL_DECISION` Notification contract.

### Requirement findings

The audit found no approved requirement specifying:

- which decisions create a Notification;
- whether approval, revision request, rejection, cancellation or withdrawal
  are separate cases;
- whether the recipient is the submitting Mangaka or all series
  contributors;
- how comments or decision reasons appear in the message;
- the related entity and Bell route;
- transaction and duplicate behavior.

### Implementation findings

No complete `PROPOSAL_DECISION` Notification flow was confirmed.

No runtime evidence exists for:

- correct recipient;
- decision-specific message;
- Bell navigation;
- `read_at_utc`;
- duplicate rejection;
- transaction persistence.

### Leader confirmation required

The Leader must confirm:

1. Which decision codes trigger a Notification.
2. Whether each decision uses the same title/message format.
3. Whether the recipient is only the proposal submitter or a wider contributor
   set.
4. Whether the Bell opens the proposal detail, series page or dashboard.
5. Whether repeated decisions or reopened proposals may create another
   Notification.
6. Which Audit Event and state transition must commit with the Notification.

### Status

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

---

## 3. `BOARD_DECISION`

### Source findings

The repository contains board-decision-related UI and workflow references,
including:

- Board Decision navigation and page components;
- Editorial Board decision queues;
- board poll, voting and finalization workflows;
- board result application to proposal or series state.

PR #82 already implements `BOARD_POLL` for the event where a Chief opens a
real poll.

`BOARD_DECISION` would represent a different event and must not be inferred
from `BOARD_POLL`.

### Requirement findings

The audit found no approved requirement specifying:

- whether `BOARD_DECISION` is a valid Notification type for the current
  deployed database;
- whether it is created when a poll closes, when a Chief finalizes a decision
  or when the result is applied;
- whether recipients include Mangaka, board members, editors or contributors;
- the related entity and route;
- the handling of ties, cancelled polls and inapplicable results;
- transaction and deduplication rules.

### Implementation findings

No `BOARD_DECISION` constant, creation flow, Bell route or runtime evidence
was approved for PR #82.

A new type must not be introduced merely because a Board Decision page exists.

### Leader confirmation required

The Leader must confirm:

1. Whether the approved type name is actually `BOARD_DECISION`.
2. Whether the deployed Notification CHECK constraint supports it.
3. The exact trigger:
   - poll close;
   - decision finalization;
   - decision application;
   - another event.
4. Recipient roles and participant scope.
5. Treatment of approved, rejected, tied and cancelled outcomes.
6. Related entity and Bell destination.
7. Transaction and duplicate rules.

### Status

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

---

## 4. `TASK_REVIEW`

### Source findings

The repository contains Task Review workflows, including:

- Assistant task output submission;
- Mangaka task-review API boundaries;
- Mangaka Review Submissions UI;
- task approval or completion;
- return-for-rework behavior;
- task cancellation.

These source references represent multiple different business events.

They do not identify which event should use `TASK_REVIEW`.

### Requirement findings

The audit found no approved requirement specifying whether `TASK_REVIEW`
means:

- Assistant submits output for Mangaka review;
- Mangaka approves the submitted output;
- Mangaka returns the task for rework;
- Mangaka cancels a task during review;
- more than one of these events.

The following are also unresolved:

- recipient role;
- title/message per outcome;
- related task or output entity;
- Bell destination;
- transaction boundary;
- duplicate behavior.

### Implementation findings

PR #82 implements `TASK_ASSIGNMENT`.

It does not implement or claim a completed `TASK_REVIEW` flow.

No runtime evidence exists for a `TASK_REVIEW` Notification.

### Leader confirmation required

The Leader must separately define:

1. Notification when the Assistant submits work.
2. Notification when the Mangaka approves work.
3. Notification when the Mangaka requests rework.
4. Notification when the task is cancelled during review.
5. Recipient for each event.
6. Related entity and Bell route for each event.
7. Whether repeated submission after rework creates a new Notification.
8. Which task/output/Audit Event writes share the transaction.

### Status

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

---

## 5. `CHAPTER_DECISION`

### Source findings

The repository contains chapter editorial-review decision behavior, including:

- Tantou Editor review pages;
- chapter approval;
- revision request;
- chapter cancellation;
- decision comments and optional markup;
- chapter status transitions.

PR #82 already implements `CHAPTER_REVIEW` for Mangaka submission of a
Chapter to Tantou review.

`CHAPTER_DECISION` would be a later event and must be specified separately.

### Requirement findings

The audit found no approved requirement confirming:

- whether `CHAPTER_DECISION` is an approved deployed Notification type;
- which chapter decisions trigger it;
- whether approval, revision and cancellation use one type;
- who receives it;
- whether the recipient is the submitting Mangaka, all active Mangaka
  contributors or another participant set;
- the Bell route;
- duplicate and transaction rules.

### Implementation findings

No complete `CHAPTER_DECISION` vertical slice was confirmed.

No Notification creation, Bell route or runtime evidence is claimed.

### Leader confirmation required

The Leader must confirm:

1. Whether the approved type name is `CHAPTER_DECISION`.
2. Whether the deployed Notification CHECK constraint supports it.
3. Which outcomes create a Notification:
   - approved;
   - revision requested;
   - cancelled.
4. Recipient participant scope.
5. Inclusion of decision comments and markup information.
6. Related entity and Bell destination.
7. Duplicate behavior after resubmission and another review decision.
8. Transaction with Chapter status and Audit Event writes.

### Status

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

---

## 6. `RANKING_WARNING`

### Source findings

The focused audit produced no source reference that creates or handles a
`RANKING_WARNING` Notification.

The project contains Ranking pages and ranking computation, but that does not
define a warning event.

Ranking results also do not automatically cancel a Series.

### Requirement findings

The audit found no approved requirement defining:

- warning threshold;
- ranking period;
- comparison rule;
- minimum number of ranking entries;
- recipient;
- warning frequency;
- cooldown;
- related entity;
- Bell route;
- Audit Event;
- duplicate rule.

### Implementation findings

No trigger, handler, repository write, Notification plan, Bell route or runtime
test exists for `RANKING_WARNING`.

Implementing this type without a threshold and cadence would risk generating
uncontrolled or duplicate Notifications.

### Leader confirmation required

The Leader must confirm:

1. The exact warning condition.
2. Whether the warning is based on:
   - rank position;
   - score;
   - rank drop;
   - consecutive periods;
   - another rule.
3. Which ranking period is used.
4. Recipient roles and participant scope.
5. Whether warnings repeat every calculation or use a cooldown.
6. Related entity and Bell destination.
7. Required Audit Event.
8. Transaction and duplicate rules.

### Status

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

---

## 7. `PUBLICATION_SCHEDULE`

### Source findings

The repository contains publication-planning and scheduling source,
including:

- Publication Schedule API controller;
- publication schedule calendar queries;
- publication schedule repository;
- chapter scheduling and rescheduling;
- on-hold and release workflows;
- Mangaka and Tantou scheduling UI/API clients.

These are multiple separate publication events.

They do not define one specific `PUBLICATION_SCHEDULE` Notification contract.

### Requirement findings

The audit found no approved requirement specifying whether the Notification is
created for:

- first scheduling;
- rescheduling;
- putting a Chapter on hold;
- returning a Chapter to schedule;
- release;
- overdue or upcoming release reminders;
- frequency mismatch warnings.

The following are also unresolved:

- recipient;
- title/message;
- related entity;
- Bell route;
- transaction boundary;
- duplicate behavior.

### Implementation findings

No complete `PUBLICATION_SCHEDULE` Notification vertical slice was confirmed
for PR #82.

The presence of publication scheduling source is not sufficient to add a
Notification to every scheduling action.

### Leader confirmation required

The Leader must confirm:

1. Which publication events create Notifications.
2. Recipient role and participant scope for each event.
3. Whether scheduling by Mangaka notifies Tantou Editor, scheduling by Tantou
   notifies Mangaka, or both.
4. Whether reminders are event-driven or scheduled background work.
5. Related entity and Bell destination.
6. Reschedule and reminder deduplication rules.
7. Transaction requirements with Chapter schedule and Audit Event writes.

### Status

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`

---

## Summary matrix

| Type | Related source exists | Approved Notification requirement found | Implementation status |
|---|---:|---:|---|
| `PROPOSAL_REVIEW` | Yes | No | `NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED` |
| `PROPOSAL_DECISION` | Yes | No | `NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED` |
| `BOARD_DECISION` | Yes | No | `NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED` |
| `TASK_REVIEW` | Yes | No | `NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED` |
| `CHAPTER_DECISION` | Yes | No | `NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED` |
| `RANKING_WARNING` | No focused Notification source found | No | `NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED` |
| `PUBLICATION_SCHEDULE` | Yes | No | `NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED` |

## PR #82 scope decision

PR #82 must not implement these seven flows without Leader confirmation.

Current approved PR #82 flows remain:

- `TASK_ASSIGNMENT`;
- `BOARD_POLL`;
- `CHAPTER_REVIEW`;
- existing `SYSTEM_MESSAGE` usages that remain valid outside removed scope.

No claim is made that all database-supported Notification types are complete.

## Validation status

- Requirement/source audit: COMPLETE
- Production source changes for these seven types: NONE
- Database changes: NONE
- Notification CHECK constraint changes: NONE
- Stored procedure changes: NONE
- Bell routes added for these seven types: NONE
- Runtime mutation tests: NOT EXECUTED
- Runtime PASS claimed: NO
- Leader scope confirmation: REQUIRED

## Final result

Each of the seven audited type names is recorded as:

`NOT IMPLEMENTED - REQUIREMENT/SCOPE CONFIRMATION REQUIRED`
