# PR #82 Leader Feedback Follow-up

**Date:** 2026-07-14
**Branch:** `feature/notification-remaining-flows`
**PR:** `#82 - feat(notification): complete remaining notification flows and editorial bell`

## 1. Scope of this follow-up

This follow-up addresses the Leader feedback concerning:

1. The source and scope of Publication Frequency Change Request.
2. Missing runtime evidence for Task Assignment Notification.
3. Redundant explicit transaction usage around a single EF Core `SaveChangesAsync`.
4. Editorial Board Bell visibility.
5. Invalid success evidence that did not display the Bell.

## 2. Publication Frequency Change Request traceability

The flow was not invented during implementation. It is traceable to existing project artifacts that predate the implementation commit:

- `BR-PUB-014`
- `BR-NOTIF-013`
- `FR-PUB-014`
- `FR-PUB-015`
- `FR-NOTIF-013`
- `FR-NOTIF-014`
- `US-MANGAKA-025`
- Notification type code `PUBLICATION_FREQUENCY_REQUEST`

However, this flow was not a direct task explicitly assigned by the Leader. It was included while completing remaining notification flows.

This distinction must be stated clearly:

- It exists in project requirements.
- It must not be represented as a directly assigned task.
- Final PR scope remains subject to Leader confirmation.

## 3. Transaction correction

### Updated file

`src/MangaManagementSystem.Infrastructure/Repositories/PublicationFrequencyRequestRepository.cs`

### Change

Removed the redundant explicit transaction calls:

- `BeginTransactionAsync`
- `CommitAsync`
- `RollbackAsync`

The flow performs EF Core entity additions for:

- `Notification`
- `AuditEvent`

and persists them through one `SaveChangesAsync` call.

EF Core already executes a single `SaveChangesAsync` atomically when the provider supports transactions. Therefore, the additional explicit transaction did not provide extra atomicity for this flow.

### Transactions intentionally retained elsewhere

Explicit transactions remain necessary in flows that combine stored procedures or raw SQL with EF Core writes:

- Single Task Assignment
- Task Reassignment
- Quick Select Assignment
- Board Poll creation

Those flows require all stored procedure/raw SQL and EF notification operations to commit or roll back together.

## 4. Single Task Assignment runtime validation

### Test context

- Mangaka: `TestMangaka1`
- Assistant: `TestAssistant1`
- Series: `Notification Frequency Runtime Test`
- Chapter: `Chapter 1`
- Page: `Page 1`
- Task type: `BACKGROUND`
- Task ID: `957a624d-8de7-4d55-b0fe-9a69aebb51dd`

### UI result

- Task creation displayed `Task assigned successfully!`.
- Active Task count changed from `0` to `1`.
- Assistant Dashboard displayed the assigned task.
- Assistant Bell displayed unread badge `1`.
- Bell dropdown displayed `New Task Assignment`.
- Clicking the notification navigated to:

  `/assistant/task/957a624d-8de7-4d55-b0fe-9a69aebb51dd`

- Task Detail displayed the correct series, chapter, page, task type and description.
- Bell unread badge disappeared.
- Notification changed to `Read`.

### Database result

For Task ID `957a624d-8de7-4d55-b0fe-9a69aebb51dd`:

- `notification_count = 1`
- `unread_count = 0`
- `read_count = 1`
- `notification_type_code = TASK_ASSIGNMENT`
- `related_entity_type = ChapterPageTask`
- `related_entity_id` matched the created task ID

Result: **PASS**

## 5. Editorial Board Bell runtime validation

### Test context

- Board account: `TestBoardMember2`
- Role: `Editorial Board Member`
- Poll ID: `3f0dfd19-01c4-441c-b647-7c850f9b7c15`

### UI result

- Editorial Dashboard displayed the Bell.
- Bell displayed unread badge `1`.
- Dropdown displayed `New Board Poll`.
- Notification message stated that a new editorial board poll was awaiting a vote.
- After selecting `Mark as read`:
  - the badge disappeared;
  - dropdown displayed `You are all caught up`;
  - notification displayed status `Read`.

### Database result

For `TestBoardMember2` and Poll ID `3f0dfd19-01c4-441c-b647-7c850f9b7c15`:

- `notification_count = 1`
- `unread_count = 0`
- `read_count = 1`
- `notification_type_code = BOARD_POLL`
- `related_entity_type = SeriesBoardPoll`
- `related_entity_id` matched the poll ID

Result: **PASS**

## 6. Evidence correction

The earlier screenshot that did not show the Editorial Board Bell must not be used as success evidence.

The final report must only use screenshots that visibly show:

- the signed-in account;
- the user role;
- the Bell;
- the unread badge where applicable;
- the notification dropdown;
- the read state after interaction.

Any earlier image without the Bell may only be labeled as `Before fix`, or it must be removed entirely.

## 7. Build validation

Final full solution build completed successfully after stopping the runtime API and Web processes:

- Build errors: `0`
- Build warnings: `0`
- Build duration: `2.8 seconds`

The earlier runtime build showed existing analyzer warnings. The final clean verification build completed without errors or warnings.

## 8. Current status

Completed:

- Transaction correction.
- Single Task Assignment runtime validation.
- Task Assignment Bell navigation validation.
- Task Assignment read-state database validation.
- Editorial Board Bell visibility validation.
- Board Poll read-state database validation.
- Evidence criteria corrected.

Pending:

- Commit and push the transaction correction and revision note.
- Update PR description.
- Rebuild the final Leader report using only current verified evidence.