# Task Assignment and Board Poll Notifications

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

- `PUBLICATION_FREQUENCY_REQUEST`
- Proposal review and decision notifications
- Chapter review and decision notifications
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

TASK_ASSIGNMENT remains build-validated but runtime-pending.

The following paths were not executed during this runtime session:

1. Single task creation.
2. Quick Select task creation.
3. Task reassignment.
4. Bell navigation to the Assistant task page.
5. Forced transaction rollback when notification insertion fails.
