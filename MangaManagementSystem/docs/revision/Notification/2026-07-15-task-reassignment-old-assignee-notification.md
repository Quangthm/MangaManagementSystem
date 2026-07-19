# PR #82 - Task Reassignment Old-Assignee Notification

Date: 2026-07-15

Branch: `feature/notification-remaining-flows`

Starting HEAD:

`e1febbe2fe5831cb880fb16b3ec07daa77aeeb98`

## Leader requirement

The latest Leader feedback requires Task Reassignment to notify both
Assistants:

1. The original Assistant must know that the original task was cancelled and
   reassigned.
2. The original Assistant's notification must contain the reassignment reason.
3. The replacement Assistant must receive the new task assignment
   notification.
4. The original task cancellation, replacement task creation, old-assignee
   Notification, new-assignee Notification and Audit Events must share one
   transaction boundary.
5. No new notification type may be added to the database.
6. The notification CHECK constraint, schema, migrations, triggers and stored
   procedures must not be changed.

## Previous behavior

Before this correction, Task Reassignment created only one Notification:

- recipient: replacement Assistant;
- type: `TASK_ASSIGNMENT`;
- related entity: replacement `ChapterPageTask`.

The original Assistant received no Notification.

Previous runtime evidence that treated an old-assignee notification count of
zero as PASS is superseded by the latest Leader requirement and must not be
used as final evidence.

## Source file changed

`src/MangaManagementSystem.Application/Services/ChapterPageTaskService.cs`

No database file, migration, CHECK constraint, trigger or stored procedure was
changed.

## Notification type selection

The implementation reuses the existing database-supported type:

`TASK_ASSIGNMENT`

No new notification type was introduced.

The type is reused for the original Assistant because the existing
Notification Bell already supports:

- notification type: `TASK_ASSIGNMENT`;
- related entity type: `ChapterPageTask`;
- related entity ID: task ID;
- route: `/assistant/task/{taskId}`.

The title and message distinguish the cancellation/reassignment notice from a
new assignment.

## Original Assistant notification

Recipient:

- the original task's `AssignedToUserId`.

Values:

- type: `TASK_ASSIGNMENT`;
- title: `Task Reassigned`;
- message:
  `Your task was cancelled and reassigned to another assistant. Reason: {reason}`;
- related entity type: `ChapterPageTask`;
- related entity ID: original task ID;
- Bell route: `/assistant/task/{originalTaskId}`.

The reason is taken from the validated and trimmed
`ReassignChapterPageTaskRequest.Reason`.

The request already enforces:

- reason is required;
- trimmed reason length must not exceed 500 characters.

## Replacement Assistant notification

Recipient:

- `ReassignChapterPageTaskRequest.NewAssignedToUserId`.

Values:

- type: `TASK_ASSIGNMENT`;
- title: `New Task Assignment`;
- message: `A production task has been reassigned to you.`;
- related entity type: `ChapterPageTask`;
- related entity ID: replacement task ID;
- Bell route: `/assistant/task/{replacementTaskId}`.

## Transaction boundary

`ChapterPageTaskService.ReassignTaskAsync` uses the existing
`IUnitOfWork` transaction.

Sequence:

1. Begin the transaction on the shared `ApplicationDbContext`.
2. Execute `AssignToDifferentUserAsync`.
3. The existing SQL/stored-procedure flow cancels the original task, creates
   the replacement task, preserves Page Region links and writes Audit Events.
4. Add the original Assistant's reassignment Notification through EF.
5. Add the replacement Assistant's assignment Notification through EF.
6. Call `IUnitOfWork.SaveChangesAsync`.
7. `UnitOfWork.SaveChangesAsync` calls
   `ApplicationDbContext.SaveChangesAsync`.
8. Commit only after all task, audit and Notification operations succeed.
9. On exception, call `RollbackTransactionAsync` and clear the shared Change
   Tracker.

The two Notifications are saved before the shared transaction is committed.

## Runtime test data

Mangaka account:

`TestMangaka1`

Original Assistant:

`TestAssistant1`

Replacement Assistant:

`TestAssistant2`

Reassignment reason:

`PR82 runtime validation: verify old-assignee cancellation notification.`

Original task:

`957A624D-8DE7-4D55-B0FE-9A69AEBB51DD`

Replacement task:

`8519E5E9-DF22-403C-8623-FD9D7977F491`

Task type:

`BACKGROUND`

Task title:

`BACKGROUND Task for Full page`

## Runtime action result

The reassignment action was executed exactly once from the Mangaka review
screen.

The Web UI returned:

`Task reassigned successfully`

The original task became `CANCELLED`.

The replacement task was created for `TestAssistant2` with status `ASSIGNED`.

## Original Assistant Bell evidence

Account:

`TestAssistant1`

Before opening the Notification:

- Bell unread badge: `1`;
- Assistant Dashboard assigned count: `0`;
- original `BACKGROUND` task status: `CANCELLED`.

Bell dropdown values:

- title: `Task Reassigned`;
- message:
  `Your task was cancelled and reassigned to another assistant. Reason: PR82 runtime validation: verify old-assignee cancellation notification.`;
- unread state: confirmed.

After clicking the Notification:

- route:
  `/assistant/task/957a624d-8de7-4d55-b0fe-9a69aebb51dd`;
- task type: `BACKGROUND`;
- task status: `CANCELLED`;
- page warning: `This task has been cancelled.`;
- Bell unread badge disappeared.

Database later confirmed that `read_at_utc` was populated.

## Replacement Assistant Bell evidence

Account:

`TestAssistant2`

Before opening the Notification:

- Bell unread badge: `1`;
- Assistant Dashboard assigned count: `2`;
- replacement `BACKGROUND` task status: `ASSIGNED`.

Bell dropdown values:

- title: `New Task Assignment`;
- message: `A production task has been reassigned to you.`;
- unread state: confirmed.

After clicking the Notification:

- route:
  `/assistant/task/8519e5e9-df22-403c-8623-fd9d7977f491`;
- task type: `BACKGROUND`;
- task status: `ASSIGNED`;
- page message:
  `This task is assigned to you and ready to work on.`;
- Bell unread badge disappeared.

Database later confirmed that `read_at_utc` was populated.

## Database task evidence

The evidence query was READ-ONLY.

Database target:

- server: `localhost`;
- database: `MangaManagementDB`.

Original task row:

- task ID:
  `957A624D-8DE7-4D55-B0FE-9A69AEBB51DD`;
- assignee: `TestAssistant1`;
- status: `CANCELLED`;
- type: `BACKGROUND`.

Replacement task row:

- task ID:
  `8519E5E9-DF22-403C-8623-FD9D7977F491`;
- assignee: `TestAssistant2`;
- status: `ASSIGNED`;
- type: `BACKGROUND`.

Task acceptance counts:

- original task count: `1`;
- replacement task count: `1`;
- original cancelled count: `1`;
- replacement assigned count: `1`;
- preserved task properties count: `1`.

## Page Region evidence

Original task Page Region:

`6BF3B833-B84F-47B6-B66A-08DEE1B6C591`

Replacement task Page Region:

`6BF3B833-B84F-47B6-B66A-08DEE1B6C591`

Acceptance counts:

- original region count: `1`;
- replacement region count: `1`;
- shared region count: `1`.

This confirms that the replacement task preserved the original task's Page
Region assignment.

## Notification database evidence

Original Assistant Notification:

- recipient: `TestAssistant1`;
- type: `TASK_ASSIGNMENT`;
- title: `Task Reassigned`;
- related entity type: `ChapterPageTask`;
- related entity ID:
  `957A624D-8DE7-4D55-B0FE-9A69AEBB51DD`;
- created at UTC: `2026-07-15 16:33:46`;
- read at UTC: `2026-07-15 16:36:42`.

Replacement Assistant Notification:

- recipient: `TestAssistant2`;
- type: `TASK_ASSIGNMENT`;
- title: `New Task Assignment`;
- related entity type: `ChapterPageTask`;
- related entity ID:
  `8519E5E9-DF22-403C-8623-FD9D7977F491`;
- created at UTC: `2026-07-15 16:33:46`;
- read at UTC: `2026-07-15 16:39:24`.

Notification acceptance counts:

- original Assistant expected Notification count: `1`;
- original Assistant read Notification count: `1`;
- replacement Assistant expected Notification count: `1`;
- replacement Assistant read Notification count: `1`;
- original Assistant wrongly linked to replacement task: `0`;
- replacement Assistant wrongly received original reassignment notice: `0`.

An older `New Task Assignment` Notification dated `2026-07-14` belongs to the
original task's initial assignment. It is not a duplicate of the reassignment
Notifications created on `2026-07-15`.

## Audit Event evidence

Cancellation Audit Event:

- audit event ID: `50`;
- actor: `TestMangaka1`;
- actor role: `Mangaka`;
- action code: `CHAPTER_PAGE_TASK_CANCELLED`;
- entity type: `ChapterPageTask`;
- entity ID:
  `957A624D-8DE7-4D55-B0FE-9A69AEBB51DD`;
- reason was present in `detail_json`.

Reassignment Audit Event:

- audit event ID: `51`;
- actor: `TestMangaka1`;
- actor role: `Mangaka`;
- action code:
  `CHAPTER_PAGE_TASK_ASSIGNED_TO_DIFFERENT_USER`;
- entity type: `ChapterPageTask`;
- entity ID:
  `957A624D-8DE7-4D55-B0FE-9A69AEBB51DD`;
- replacement task ID was present in `detail_json`.

Audit acceptance counts:

- cancellation Audit Event count: `1`;
- reassignment Audit Event count: `1`;
- Audit Event containing reason count: `1`;
- Audit Event containing replacement task ID count: `1`.

## Transaction persistence evidence

The same completed reassignment produced all expected persisted records:

- original task changed to `CANCELLED`;
- replacement task created as `ASSIGNED`;
- replacement task preserved the original Page Region;
- original Assistant Notification created;
- replacement Assistant Notification created;
- cancellation Audit Event created;
- reassignment Audit Event created.

The two Notifications share the same creation timestamp:

`2026-07-15 16:33:46`

The two Audit Events were written immediately before them:

- cancellation: `2026-07-15 16:33:45.7115619`;
- reassignment: `2026-07-15 16:33:45.7187122`.

This confirms successful persistence of the complete transaction result.

A forced failure was not injected, so rollback behavior was not mutated or
manufactured for evidence.

## Duplicate protection

The original task is now `CANCELLED`.

Source validation rejects reassignment when the original task is no longer in
an assignable status.

The runtime action was intentionally executed only once to avoid creating an
unnecessary second database mutation.

The database evidence confirms exactly one matching old-assignee
reassignment Notification, one matching replacement-assignee Notification,
one cancellation Audit Event and one reassignment Audit Event for this
reassignment.

A deliberate repeated API mutation was not executed.

Duplicate replay status:

`NOT EXECUTED - EXISTING STATUS GUARD AND DATABASE COUNTS VERIFIED`

## Static validation

`git diff --check`:

`PASS`

Application Release build:

- `MangaManagementSystem.Domain`: succeeded;
- `MangaManagementSystem.Application`: succeeded;
- build result: succeeded;
- elapsed time: 2.0 seconds.

Final full-solution Release build after all source and documentation work:

- all five projects built from `bin\Release\net8.0`;
- errors: `0`;
- warnings: `0`;
- result: succeeded;
- elapsed time: 3.46 seconds.

## Database safety

All evidence queries used for this validation were READ-ONLY.

No operation changed:

- database schema;
- migration;
- CHECK constraint;
- trigger;
- stored procedure;
- production data for evidence fabrication.

The only database mutation was the legitimate reassignment action performed
through the application UI.

## Failure-path status

Forced rollback or failure injection:

`NOT EXECUTED`

No database value, CHECK constraint, trigger, stored procedure or production
source was modified to manufacture a failure.

## Current status

- Leader requirement analysis: COMPLETE
- Source implementation: COMPLETE
- Notification type audit for this flow: COMPLETE
- Application Release build: PASS
- Full-solution Release build after source change: PASS
- Original Assistant runtime test: PASS
- Replacement Assistant runtime test: PASS
- Bell badge and dropdown evidence: PASS
- Bell route evidence: PASS
- `read_at_utc` evidence: PASS
- Database task evidence: PASS
- Page Region preservation: PASS
- Notification recipient isolation: PASS
- Audit Event evidence: PASS
- Successful transaction persistence: PASS
- Deliberate duplicate replay: NOT EXECUTED
- Forced rollback/failure injection: NOT EXECUTED
- Final build after all documentation: PASS
- Commit and push: NOT EXECUTED
- PDF update: NOT EXECUTED

Database acceptance result:

`task_b_database_result = PASS`

Final Task B status:

`IMPLEMENTED AND RUNTIME VALIDATED - FORCED FAILURE AND DELIBERATE DUPLICATE REPLAY NOT EXECUTED`
