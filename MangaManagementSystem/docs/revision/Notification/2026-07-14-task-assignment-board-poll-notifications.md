# Task Assignment and Board Poll Notifications

> **Superseded reassignment evidence notice - 2026-07-15**
>
> The latest Leader requirement now requires a cancellation/reassignment
> Notification for the original Assistant, including the reassignment reason.
> Any earlier section in this document that treats an old-assignee notification
> count of zero as PASS is historical evidence only and must not be used as the
> final PR #82 acceptance result.
>
> The corrected implementation and completed runtime evidence are documented in
> `2026-07-15-task-reassignment-old-assignee-notification.md`.

## Date

2026-07-14

## Branch and baseline

- Branch: `feature/notification-remaining-flows`
- Baseline commit: `4fb8361`
- Baseline source: `origin/main`
- No merge into `main` was performed.

## Scope completed

### 1. Task assignment notification

Implemented `TASK_ASSIGNMENT` notifications for:

- Creating one chapter page task.
- Creating multiple tasks through Quick Select.
- Reassigning a task to another assistant.

Each created or replacement task generates one notification for the assigned user.

Notification relationship:

- Type: `TASK_ASSIGNMENT`
- Related entity type: `ChapterPageTask`
- Related entity ID: the newly created task ID

### 2. Transaction consistency for task creation

For single task creation and reassignment:

- Application owns the business decision to create the notification.
- Raw stored-procedure commands participate in the current EF transaction.
- Task changes and notification insertion commit together.
- Any failure rolls back the complete operation.

For Quick Select:

- Application creates one notification intent per planned task.
- Infrastructure inserts regions, tasks, audit events, and notifications inside the existing Quick Select transaction.
- A single `SaveChangesAsync` persists the EF entities before commit.

### 3. Removed invalid controller notification behavior

Removed controller-side notification creation that used unsupported notification type values, including:

- `NEW_TASK_ASSIGNED`
- `TASK_ASSIGNED`
- Unsupported task completion, rework, and cancellation notification attempts

Controllers now delegate the business operation to Application services.

### 4. Notification Bell navigation

For valid `TASK_ASSIGNMENT` notifications:

- The Bell validates the notification type and related entity.
- Unread notifications are marked as read through the existing API first.
- Navigation occurs only after mark-as-read succeeds.
- Destination route: `/assistant/task/{TaskId}`

No Notification API client contract was changed.

### 5. Board poll notification

When an Editorial Board Chief opens a real board poll through the MediatR flow:

- Every active user whose exact role is `Editorial Board Member` receives one notification.
- The Chief is explicitly excluded.
- Duplicate recipient IDs are removed.
- Poll creation, proposal/series updates, audit append, and notification insertion share one transaction.

Notification relationship:

- Type: `BOARD_POLL`
- Related entity type: `SeriesBoardPoll`
- Related entity ID: the newly created poll ID

The placeholder `BoardPollsController` was not modified because the active UI flow uses `EditorialBoardController` and `OpenSeriesBoardPollCommand`.

## Database impact

- No schema change.
- No migration.
- No new stored procedure.
- No stored procedure definition was modified.
- Existing EF mappings and notification table were reused.

## Validation performed

- `git diff --check`: passed.
- Full solution build with `--no-restore`: succeeded.
- Domain build: succeeded.
- Application build: succeeded.
- Infrastructure build: succeeded.
- API build: succeeded.
- Web build: succeeded.
- Invalid controller notification type search returned no matches.
- Notification API clients remained unchanged.
- Branch and changed-file scope were verified before commit.

## Runtime validation still required

Database-backed manual tests should confirm:

1. Single task creation inserts exactly one `TASK_ASSIGNMENT`.
2. Quick Select inserts one notification per created task.
3. Reassignment links the notification to the replacement task ID.
4. Bell click marks the notification as read and opens the assistant task route.
5. Opening a board poll notifies only active Editorial Board Members.
6. A failed notification insert rolls back the related business transaction.

## Deferred notification flows

Not included in this commit:
- Proposal review and decision notifications
- Chapter decision notifications
- Ranking warning notifications
- Publication schedule notifications

These require separate validated business flows or recipient rules.

## Runtime validation update - Board Poll

Runtime validation was completed for the real Board Poll workflow.

### Workflow result

- TestMangaka1 created and submitted Notification Frequency Runtime Test.
- TestEditor1 claimed the proposal and passed it to the Editorial Board.
- TestBoardChief1 opened a START_SERIALIZATION poll.
- TestBoardMember1 voted APPROVE.
- TestBoardChief1 closed the poll.
- Final result: Approved.
- Poll status: CLOSED.
- Series status: SERIALIZED.
- Official frequency: WEEKLY.

### Notification database evidence

- Active Editorial Board Member count: 5.
- BOARD_POLL notifications created: 5.
- Distinct recipients: 5.
- Invalid recipient count: 0.
- Duplicate-recipient query returned no rows.
- Every recipient had role Editorial Board Member and status ACTIVE.
- Related entity type was SeriesBoardPoll.
- Related entity ID matched the created poll.
- Editorial Board Chief was excluded.

### Notification Bell evidence

- TestBoardMember1 saw one unread New Board Poll notification.
- Mark as read removed the unread badge.
- The notification remained visible with status Read.
- Database read_at_utc was populated.
- Final read-status verification returned PASS.

### Task Assignment runtime status

Runtime validation has now been completed for the Quick Select task-assignment path.

The following paths were pending at that historical checkpoint:

1. Single task creation.
2. Task reassignment.
3. Forced transaction rollback when notification insertion fails.

## Runtime validation update - Quick Select Task Assignment

### Test context

Series:

- Title: `Notification Frequency Runtime Test`
- Series ID: `5da0dfb7-dcbe-4466-a662-323fdde5e286`

Chapter and page:

- Chapter: `1`
- Chapter ID: `1f1783c9-e954-4d4e-c23f-08dee1b53155`
- Chapter Page ID: `3067c703-75d9-441f-fb7f-08dee1b62017`
- Current Page Version ID: `bc808d3c-09d6-473b-1d0a-08dee1b62020`
- Page number: `1`
- Version number: `1`

Actor:

- Username: `TestMangaka1`
- User ID: `43efbd0d-f2ce-4788-af1d-9c51a5530d83`
- Role: `Mangaka`

Recipient:

- Username: `TestAssistant1`
- User ID: `fa178e8d-fdd0-4e1c-ad27-cc7bea9ac56e`
- Role: `Assistant`
- User status: `ACTIVE`
- Active contributor of the exact series: `YES`

### Quick Select form values

- Task type: `SHADING`
- Task title prefix: `Quick Select Runtime Shading`
- Generated task title: `Quick Select Runtime Shading - Page 1`
- Priority: `2`
- Due date: `2026-07-22`
- Compensation: `100.00`
- Description: `Quick Select runtime notification validation for Chapter 1 Page 1.`

### Before-create database evidence

Immediately before clicking `CREATE TASKS`:

- Existing task count linked to Page 1: `1`
- Matching runtime task count: `0`
- Matching runtime notification count: `0`
- Matching runtime Audit Event count: `0`
- Existing `FULL_PAGE` region count: `1`

The existing task was:

- Type: `BACKGROUND`
- Status: `ASSIGNED`
- Recipient: `TestAssistant1`

Result: PASS.

### UI creation evidence

After clicking `CREATE TASKS` once:

- Success message displayed: `Created 1 task(s).`
- Assigned task count increased from `1` to `2`.
- The existing `BACKGROUND` task remained unchanged.
- The new `SHADING` task appeared.
- The new task title was `Quick Select Runtime Shading - Page 1`.
- Recipient displayed as `TestAssistant1`.
- Priority displayed as `2`.
- Compensation displayed as `100.00`.

Result: PASS.

### Created task database evidence

Exactly one matching task was created:

- Task ID: `14f56a93-63b5-4eaa-afec-1fea1001762d`
- Assigned user: `TestAssistant1`
- Assigned user ID: `fa178e8d-fdd0-4e1c-ad27-cc7bea9ac56e`
- Type: `SHADING`
- Status: `ASSIGNED`
- Title: `Quick Select Runtime Shading - Page 1`
- Priority: `2`
- Due date: `2026-07-22`
- Compensation: `100.00`
- Related Chapter Page ID: `3067c703-75d9-441f-fb7f-08dee1b62017`
- Related Page Version ID: `bc808d3c-09d6-473b-1d0a-08dee1b62020`
- Related Page Region ID: `6bf3b833-b84f-47b6-b66a-08dee1b6c591`
- Page Region type: `FULL_PAGE`

Result: PASS.

### Notification database evidence

Exactly one matching notification was created:

- Notification ID: `8322d7d0-dd45-4ea2-3d90-08dee264ae82`
- Recipient: `TestAssistant1`
- Notification type: `TASK_ASSIGNMENT`
- Title: `New Task Assignment`
- Message: `You have been assigned a new production task.`
- Related entity type: `ChapterPageTask`
- Related entity ID: `14f56a93-63b5-4eaa-afec-1fea1001762d`
- Initial state: unread

The Notification related entity ID matched the newly created Task ID exactly.

Result: PASS.

### Audit Event evidence

Exactly one matching Audit Event was created:

- Audit Event ID: `46`
- Actor user ID: `43efbd0d-f2ce-4788-af1d-9c51a5530d83`
- Actor username: `TestMangaka1`
- Action code: `CHAPTER_PAGE_TASK_CREATED`
- Entity type: `ChapterPageTask`
- Entity ID: `14f56a93-63b5-4eaa-afec-1fea1001762d`
- Assigned user ID: `fa178e8d-fdd0-4e1c-ad27-cc7bea9ac56e`
- Type code: `SHADING`
- Priority level: `2`
- Compensation amount: `100`
- Page Region ID: `6bf3b833-b84f-47b6-b66a-08dee1b6c591`

Observation:

- `actor_role_name` was not populated in this Audit Event.
- The actor was still identified through `actor_user_id` and the joined username `TestMangaka1`.
- This observation must not be represented as an actor-role PASS until the expected audit contract is confirmed.

### Atomic persistence evidence

Before creation:

- Matching task count: `0`
- Matching notification count: `0`
- Matching Audit Event count: `0`
- `FULL_PAGE` region count: `1`

After successful creation:

- Matching task count: `1`
- Matching notification count: `1`
- Matching Audit Event count: `1`
- Total task count linked to Page 1 increased from `1` to `2`
- `FULL_PAGE` region count remained `1`

The existing region was reused rather than duplicated.

The task, Notification and Audit Event had the same creation timestamp:

- `2026-07-15 12:59:46`

The Quick Select repository persisted regions, tasks, Audit Events and Notifications inside one explicit EF transaction and committed only after one successful `SaveChangesAsync`.

Result: PASS for the successful transaction path.

Forced rollback runtime validation was not executed because the repository has no approved automated failure-injection mechanism. Static transaction verification is PASS.

### Assistant Bell evidence

After logging in as `TestAssistant1`:

- Bell displayed unread badge `1`.
- The Assistant dashboard displayed `2` assigned tasks.
- Both `BACKGROUND` and `SHADING` tasks were visible.
- Bell dropdown displayed one unread `New Task Assignment`.
- The previous task notification remained read.

After clicking the unread notification:

- Browser navigated to `/assistant/task/14f56a93-63b5-4eaa-afec-1fea1001762d`.
- The Task Detail page displayed the exact `SHADING` task.
- Series, Chapter 1 and Page 1 were correct.
- Status was `ASSIGNED`.
- Priority was `2`.
- One `FULL_PAGE` region was displayed.
- Bell unread badge was cleared.

Database read-state evidence:

- Notification `read_at_utc`: `2026-07-15 13:07:30`
- Read-state result: `PASS`
- Remaining unread `TASK_ASSIGNMENT` count for `TestAssistant1`: `0`

Result: PASS.

### Quick Select acceptance results

1. Active Mangaka contributor performed the assignment: PASS
2. Active Assistant contributor of the exact series was selected: PASS
3. Current page version was used: PASS
4. Exactly one new task was created: PASS
5. Exactly one Notification was created: PASS
6. Exactly one Audit Event was created: PASS
7. Notification type was `TASK_ASSIGNMENT`: PASS
8. Related entity type was `ChapterPageTask`: PASS
9. Related entity ID matched the new Task ID: PASS
10. Existing task remained unchanged: PASS
11. Existing `FULL_PAGE` region was reused: PASS
12. No duplicate runtime task was created: PASS
13. No duplicate runtime Notification was created: PASS
14. Bell displayed the unread notification: PASS
15. Bell navigated to the exact Assistant task route: PASS
16. `read_at_utc` was populated: PASS
17. Assistant unread task count became `0`: PASS
18. Successful transaction persistence: PASS
19. Forced rollback path: NOT EXECUTED
20. Audit `actor_role_name`: OBSERVATION - not populated

## Runtime validation update - Task Reassignment

### Contributor preparation

Before reassignment, the test series had only one active Assistant contributor:

- `TestAssistant1`

The reassignment flow requires the new assignee to:

- have the `Assistant` role;
- have user status `ACTIVE`;
- be an active contributor of the exact series;
- be different from the current assignee.

`TestAssistant2` was therefore added through the real Manage Series Contributors UI.

Contributor details:

- Username: `TestAssistant2`
- User ID: `6e08f0e6-d632-42d2-9f10-3506317802a3`
- Role: `Assistant`
- User status: `ACTIVE`
- Series ID: `5da0dfb7-dcbe-4466-a662-323fdde5e286`
- Series Contributor ID: `cf275607-a724-400f-8810-6ab26f642a68`
- Start date: `2026-07-15`
- End date: `NULL`

UI evidence:

- Success message displayed: `Assistant contributor added successfully.`
- Active Contributor count increased from `3` to `4`.
- Active Assistant count increased from `1` to `2`.
- `TestAssistant2` appeared as an active Assistant contributor.

Database evidence:

- `contributor_result`: `PASS`
- Active Assistants of the series:
  - `TestAssistant1`
  - `TestAssistant2`
- Active Assistant count: `2`

Result: PASS.

### Reassignment test context

Original task:

- Task ID: `14f56a93-63b5-4eaa-afec-1fea1001762d`
- Assigned user: `TestAssistant1`
- Assigned user ID: `fa178e8d-fdd0-4e1c-ad27-cc7bea9ac56e`
- Type: `SHADING`
- Status before reassignment: `ASSIGNED`
- Title: `Quick Select Runtime Shading - Page 1`
- Priority: `2`
- Due date: `2026-07-22`
- Compensation: `100.00`
- Region count: `1`
- Created by: `TestMangaka1`

New assignee:

- Username: `TestAssistant2`
- User ID: `6e08f0e6-d632-42d2-9f10-3506317802a3`
- Eligibility result: `ELIGIBLE`

Reason:

`Runtime validation: reassign SHADING task from TestAssistant1 to TestAssistant2.`

The optional updated task description was left empty so that the original description would be preserved.

### Before-reassignment evidence

Immediately before reassignment:

- Original task was assigned to `TestAssistant1`.
- Original task status was `ASSIGNED`.
- Original task region count was `1`.
- Replacement task count was `0`.
- Replacement notification count was `0`.
- `TestAssistant2` was an eligible active Assistant contributor.

Result: PASS.

### Reassignment UI evidence

After clicking `REASSIGN` once:

- Success message displayed: `Task reassigned successfully.`
- Original task for `TestAssistant1` displayed status `CANCELLED`.
- Replacement task for `TestAssistant2` displayed status `ASSIGNED`.
- Both tasks retained type `SHADING`.
- The replacement task retained the original task title and description.
- Active task count remained `2`.
- Cancelled task count increased to `1`.

The active-task count did not increase incorrectly through an extra duplicate active task.

Result: PASS.

### Original and replacement task database evidence

Original task:

- Task ID: `14f56a93-63b5-4eaa-afec-1fea1001762d`
- Assigned user: `TestAssistant1`
- Final status: `CANCELLED`

Replacement task:

- Task ID: `e6ddca17-ce19-4948-b9c7-39f24c2c1342`
- Assigned user: `TestAssistant2`
- Assigned user ID: `6e08f0e6-d632-42d2-9f10-3506317802a3`
- Type: `SHADING`
- Status: `ASSIGNED`
- Title: `Quick Select Runtime Shading - Page 1`
- Description preserved from the original task
- Priority: `2`
- Due date: `2026-07-22`
- Compensation: `100.00`

Result: PASS.

### Region-link evidence

Both tasks referenced the same Page Region:

- Page Region ID: `6bf3b833-b84f-47b6-b66a-08dee1b6c591`
- Page Version ID: `bc808d3c-09d6-473b-1d0a-08dee1b62020`
- Region type: `FULL_PAGE`

Counts:

- Original task region count: `1`
- Replacement task region count: `1`
- Shared region count: `1`

The reassignment reused the existing region rather than creating a duplicate region.

Result: PASS.

### Reassignment Notification evidence

Exactly one notification was created for the replacement task:

- Notification ID: `796e655b-d6db-46cf-3d91-08dee264ae82`
- Recipient: `TestAssistant2`
- Notification type: `TASK_ASSIGNMENT`
- Title: `New Task Assignment`
- Message: `A production task has been reassigned to you.`
- Related entity type: `ChapterPageTask`
- Related entity ID: `e6ddca17-ce19-4948-b9c7-39f24c2c1342`
- Initial state: unread
- Notification validation result: `PASS`

The related entity ID matched the replacement Task ID exactly.

Recipient validation:

- New assignee notification count: `1`
- Old assignee wrong-notification count: `0`

Result: PASS.

### Reassignment Audit Event evidence

Cancellation Audit Event:

- Audit Event ID: `48`
- Actor: `TestMangaka1`
- Actor role: `Mangaka`
- Action code: `CHAPTER_PAGE_TASK_CANCELLED`
- Entity type: `ChapterPageTask`
- Entity ID: original Task ID
- Old status: `ASSIGNED`
- New status: `CANCELLED`

Different-user assignment Audit Event:

- Audit Event ID: `49`
- Actor: `TestMangaka1`
- Actor role: `Mangaka`
- Action code: `CHAPTER_PAGE_TASK_ASSIGNED_TO_DIFFERENT_USER`
- Entity type: `ChapterPageTask`
- Entity ID: original Task ID
- Old Task ID: `14f56a93-63b5-4eaa-afec-1fea1001762d`
- New Task ID: `e6ddca17-ce19-4948-b9c7-39f24c2c1342`
- Old assignee: `TestAssistant1`
- New assignee: `TestAssistant2`
- Original task final status: `CANCELLED`
- Replacement task status: `ASSIGNED`
- Type: `SHADING`
- Reassignment reason was preserved.

Result: PASS.

### Atomic persistence evidence

The reassignment service used one explicit Unit of Work transaction.

Inside that transaction:

1. The stored procedure cancelled the original task.
2. The stored procedure created the replacement task.
3. The stored procedure copied the Page Region links.
4. The stored procedure appended the Audit Events.
5. EF inserted the new `TASK_ASSIGNMENT` Notification.
6. `SaveChangesAsync` completed before transaction commit.

Successful-result counts:

- Original cancelled count: `1`
- Replacement assigned count: `1`
- Replacement task count: `1`
- New assignee notification count: `1`
- Old assignee wrong-notification count: `0`
- Original region count: `1`
- Replacement region count: `1`
- Shared region count: `1`

Result: PASS for the successful transaction path.

Forced rollback runtime validation was not executed because the repository has no approved automated failure-injection mechanism. Static transaction verification is PASS.

### TestAssistant2 Bell evidence

After logging in as `TestAssistant2`:

- Bell displayed unread badge `1`.
- Assistant Workspace displayed one assigned `SHADING` task.
- Bell dropdown displayed `New Task Assignment`.
- Notification message displayed:
  `A production task has been reassigned to you.`
- Notification was initially unread.

After clicking the notification:

- Browser navigated to:
  `/assistant/task/e6ddca17-ce19-4948-b9c7-39f24c2c1342`
- The exact replacement task was displayed.
- Type was `SHADING`.
- Status was `ASSIGNED`.
- Series, Chapter 1 and Page 1 were correct.
- Priority was `2`.
- One `FULL_PAGE` region was displayed.
- Bell unread badge was cleared.

Database read-state evidence:

- Notification `read_at_utc`: `2026-07-15 13:42:27`
- Read-state result: `PASS`
- Remaining unread `TASK_ASSIGNMENT` count for `TestAssistant2`: `0`

Result: PASS.

### Task Reassignment acceptance results

1. New assignee had the Assistant role: PASS
2. New assignee user status was ACTIVE: PASS
3. New assignee was an active contributor of the exact series: PASS
4. New assignee differed from the original assignee: PASS
5. Original task was owned by the acting Mangaka: PASS
6. Original task transitioned from ASSIGNED to CANCELLED: PASS
7. Exactly one replacement task was created: PASS
8. Replacement task was assigned to TestAssistant2: PASS
9. Replacement task status was ASSIGNED: PASS
10. Task title and description were preserved: PASS
11. Existing Page Region was reused: PASS
12. Exactly one TASK_ASSIGNMENT Notification was created: PASS
13. Related entity ID matched the replacement Task ID: PASS
14. Old assignee received no replacement-task Notification: PASS
15. Cancellation Audit Event was created: PASS
16. Different-user assignment Audit Event was created: PASS
17. Audit actor role was Mangaka: PASS
18. Bell displayed the unread Notification: PASS
19. Bell navigated to the exact replacement task route: PASS
20. `read_at_utc` was populated: PASS
21. Unread count became `0`: PASS
22. Successful transaction persistence: PASS
23. Forced rollback runtime path: NOT EXECUTED — static transaction verification PASS


## Forced rollback validation decision

### Runtime status

`NOT EXECUTED`

The repository currently has no approved automated test project or
failure-injection mechanism for deliberately failing Notification persistence.

To avoid violating the database restrictions communicated by the Leader, the
validation did not:

- modify a database constraint;
- create or modify a trigger;
- modify a stored procedure;
- insert invalid production data;
- change production source code solely to force an artificial error;
- run an external rollback probe against the shared database.

### Static transaction verification

The Task Assignment and Task Reassignment application flows use the following
transaction sequence:

1. Begin the shared Unit of Work transaction.
2. Execute the stored procedure that creates or replaces the task and writes
   the corresponding audit information.
3. Add the `TASK_ASSIGNMENT` Notification through EF.
4. Call `SaveChangesAsync`.
5. Commit only after all operations succeed.
6. Catch any exception and call `RollbackTransactionAsync`.
7. Clear the EF Change Tracker during rollback.

Static transaction structure: `PASS`

Forced rollback runtime execution: `NOT EXECUTED`

This status is intentionally reported without claiming runtime PASS.

### Current Task Assignment runtime summary

- Single task creation: NOT IN LEADER FOLLOW-UP RUNTIME SCOPE
- Quick Select task creation: PASS
- Quick Select Notification: PASS
- Quick Select Bell navigation: PASS
- Quick Select read state: PASS
- Quick Select duplicate validation: PASS
- Quick Select successful transaction persistence: PASS
- Task reassignment: PASS
- Reassignment Notification: PASS
- Reassignment Bell navigation: PASS
- Reassignment read state: PASS
- Reassignment recipient validation: PASS
- Reassignment successful transaction persistence: PASS
- Forced transaction rollback runtime: NOT EXECUTED
- Quick Select Audit `actor_role_name`: OBSERVATION — not populated
- Reassignment Audit `actor_role_name`: PASS — `Mangaka`
