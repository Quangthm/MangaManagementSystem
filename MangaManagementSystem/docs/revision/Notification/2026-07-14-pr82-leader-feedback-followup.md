# PR #82 Leader Feedback Follow-up

**Date:** 2026-07-14
**Branch:** `feature/notification-remaining-flows`
**PR:** `#82 - feat(notification): complete remaining notification flows and editorial bell`

## 1. Scope of this follow-up

This follow-up addresses the Leader feedback concerning:

1. Missing runtime evidence for Task Assignment Notification.
2. The Unit of Work and DbContext transaction boundary.
3. Editorial Board Bell visibility.
4. Invalid success evidence that did not display the Bell.
## 2. DbContext and Unit of Work relationship

`ApplicationDbContext` is the EF Core persistence context used for entity
tracking, database access and `SaveChangesAsync`.

`IUnitOfWork` and `UnitOfWork` are not a second persistence mechanism.
`UnitOfWork` wraps the same scoped `ApplicationDbContext`, exposes repositories
that operate through that context, and provides transaction orchestration.

The current implementation confirms that:

- `UnitOfWork.SaveChangesAsync` calls `_context.SaveChangesAsync`;
- `UnitOfWork.BeginTransactionAsync` opens a transaction through
  `_context.Database`;
- `UnitOfWork.CommitTransactionAsync` commits that context transaction;
- `UnitOfWork.RollbackTransactionAsync` rolls it back and clears the same
  context Change Tracker.

Therefore, a Unit of Work save ultimately remains a DbContext save.

### EF-only repository flow

`MangakaChapterRepository` performs the Chapter Submit flow through one
`ApplicationDbContext`.

The Chapter status update, Chapter Review Notifications and Audit Event are
tracked by that context and persisted through one `SaveChangesAsync` call.

No explicit transaction is added because one EF Core `SaveChangesAsync` is the
atomic persistence boundary for this EF-only operation.

### Application-coordinated mixed SQL and EF flows

Single Task Assignment and Task Reassignment are coordinated by the
Application service through `IUnitOfWork`.

Their sequence is:

1. Begin the transaction on the shared DbContext.
2. Execute the stored procedure or SQL task operation through a repository
   using that transaction.
3. Add the EF Notification through a repository backed by the same context.
4. Call `IUnitOfWork.SaveChangesAsync`, which calls
   `ApplicationDbContext.SaveChangesAsync`.
5. Commit only after every operation succeeds.
6. Roll back and clear the shared Change Tracker when an exception occurs.

### Repository-owned explicit transactions in the current code

Quick Select and Board Poll currently encapsulate their explicit transactions
inside Infrastructure repositories by using the repositories' injected
`ApplicationDbContext` directly.

They still coordinate SQL and EF writes within one DbContext transaction, but
they do not call the `IUnitOfWork` abstraction.

The previous documentation incorrectly listed these flows as Unit of Work
flows. This documentation correction records the actual implementation and
does not perform an unrelated architectural refactor.

### Rule used for this PR

DbContext and Unit of Work must not be described as independent persistence
systems.

Use one EF `SaveChangesAsync` for an EF-only atomic operation. Use an explicit
transaction when SQL or stored-procedure changes must commit or roll back with
EF changes. When an Application service owns that orchestration, it uses
`IUnitOfWork`, whose save and transaction methods operate on the shared
`ApplicationDbContext`.

A broader refactor requiring every repository-owned mixed flow to be moved
behind `IUnitOfWork` is outside this correction and requires explicit scope
confirmation.
## 3. Single Task Assignment runtime validation

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

## 4. Editorial Board Bell runtime validation

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

## 5. Evidence correction

The earlier screenshot that did not show the Editorial Board Bell must not be used as success evidence.

The final report must only use screenshots that visibly show:

- the signed-in account;
- the user role;
- the Bell;
- the unread badge where applicable;
- the notification dropdown;
- the read state after interaction.

Any earlier image without the Bell may only be labeled as `Before fix`, or it must be removed entirely.

## 6. Build validation

Final full solution build completed successfully after stopping the runtime API and Web processes:

- Build errors: `0`
- Build warnings: `0`
- Build duration: `3.46 seconds`

The earlier runtime build showed existing analyzer warnings. The final clean verification build completed without errors or warnings.

## 7. Current status

Completed:

- Transaction correction.
- Single Task Assignment runtime validation.
- Task Assignment Bell navigation validation.
- Task Assignment read-state database validation.
- Editorial Board Bell visibility validation.
- Board Poll read-state database validation.
- Task Reassignment old-assignee and replacement-assignee runtime validation.
- Task Reassignment Bell route and read-state database validation.
- Task Reassignment transaction persistence and Audit Event validation.
- Seven Notification Types requirement and scope audit.
- Final full-solution Release build after all documentation.
- Evidence criteria corrected.

Pending:

- Commit and push the transaction correction and revision note.
- Update PR description.
- Rebuild the final Leader report using only current verified evidence.
