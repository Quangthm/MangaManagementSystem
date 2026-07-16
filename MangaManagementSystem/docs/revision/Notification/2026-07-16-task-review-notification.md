# TASK_REVIEW notification

## Date

2026-07-16

## Leader-confirmed requirement

When an Assistant submits a task for review, notify all active Mangaka contributors of that exact series.

## Trigger

- Assistant submits task work through `POST /api/assistant/tasks/{taskId}/submit-work`.
- Task transitions from `ASSIGNED` to `UNDER_REVIEW`.

## Implementation

- Added `NotificationTypeCodes.TaskReview` with value `TASK_REVIEW`.
- Extended `AssistantTaskSubmissionService.SubmitTaskWorkAsync`.
- Existing SQL commands and EF notification inserts share one EF-managed transaction.
- The exact series is resolved through task region, page version, chapter page and chapter.
- Recipients are distinct active Mangaka contributors of that exact series.
- Recipient user status must be `ACTIVE`.
- Contributor end date must be null.
- Notification type is `TASK_REVIEW`.
- Related entity type is `ChapterPageTask`.
- Related entity ID is the submitted task ID.
- Bell navigation opens `/mangaka/review-submissions`.
- No database schema or stored-procedure definition was changed.

## Source files

- `MangaManagementSystem.Application/Common/NotificationConstants.cs`
- `MangaManagementSystem.Infrastructure/Services/AssistantTaskSubmissionService.cs`
- `MangaManagementSystem.Web/Components/Shared/NotificationBell.razor`

## Build validation

- Release build completed with 0 errors.
- Build result: 68 warnings and 0 errors.
- No new warning was reported from the three TASK_REVIEW source files.

## Runtime scenario

- Series: `Notification Frequency Runtime Test`.
- Task: `BACKGROUND Task for Full page`.
- Task ID: `8519E5E9-DF22-403C-8623-FD9D7977F491`.
- Assistant: `TestAssistant2`.
- Expected Mangaka recipient: `TestMangaka1`.
- Version note: `PR82 TASK_REVIEW runtime test submission.`.

## Runtime submission result

- Task status changed from `ASSIGNED` to `UNDER_REVIEW`.
- A completed page version was created.
- The submitted version became the current page version.
- The submission flow completed successfully through the Web UI.

## Recipient validation

- Expected active Mangaka count: `1`.
- Actual notification count: `1`.
- Distinct recipient count: `1`.
- Unread notification count before click: `1`.
- Recipient: `TestMangaka1`.
- No unrelated contributor received the notification.
- No duplicate notification was created.

## Notification validation

- Notification title: `Assistant Task Submitted for Review`.
- Notification type: `TASK_REVIEW`.
- Related entity type: `ChapterPageTask`.
- Related entity ID: `8519E5E9-DF22-403C-8623-FD9D7977F491`.
- Bell message contained the correct task and series names.

## Bell and read-state validation

- `TestMangaka1` received the notification as unread.
- Clicking the notification opened `/mangaka/review-submissions`.
- The submitted task appeared in the Mangaka review queue as `UNDER_REVIEW`.
- After the click, the notification received a non-null `read_at_utc` value.
- `test_mangaka_read_count = 1`.
- `total_notification_count = 1`.

## Result

`TASK_REVIEW` implementation and runtime validation: PASS.
