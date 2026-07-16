# BOARD_DECISION notification

## Date

2026-07-16

## Leader-confirmed requirement

When a board poll closes and has a result, notify all active contributors of that exact series.

## Implementation

- Added `NotificationTypeCodes.BoardDecision`.
- Added notification creation to `FinalizeApprovalAsync`.
- Recipients are distinct users from `ActiveSeriesContributors` for the exact series.
- Recipients must have no contributor end date and an `ACTIVE` user status.
- Notification type is `BOARD_DECISION`.
- Related entity type is `SeriesBoardPoll`.
- Related entity ID is the closed board poll ID.
- Notification title and message reflect `APPROVED`, `REJECTED`, or no-decision results.
- Board poll SQL, audit event and EF notifications share one EF transaction.
- Added Bell navigation to `/notifications/board-decision/{notificationId}`.
- Added a role-neutral authenticated Board Decision notification page.
- No database schema or stored-procedure definition was changed.

## Source files

- `MangaManagementSystem.Application/Common/NotificationConstants.cs`
- `MangaManagementSystem.Infrastructure/Repositories/EditorialBoardRepository.cs`
- `MangaManagementSystem.Web/Components/Shared/NotificationBell.razor`
- `MangaManagementSystem.Web/Components/Pages/Notifications/BoardDecisionNotification.razor`

## Build validation

- Release build completed with 0 errors.
- The new BoardDecisionNotification page produced no build warning.

## Runtime scenario

- Series: `PR82 Proposal Decision Runtime Test`.
- Proposal version: `v2`.
- Tantou Editor `TestEditor1` passed the proposal to board review.
- Board Chief `TestBoardChief1` opened a `START_SERIALIZATION` poll.
- Publication frequency: `WEEKLY`.
- `TestBoardChief1` submitted one `APPROVE` vote.
- Board Chief closed the poll through Decision Center.

## Runtime workflow result

- Poll status: `CLOSED`.
- Computed result: `APPROVED`.
- Series status: `SERIALIZED`.
- Proposal status: `APPROVED`.
- Publication frequency: `WEEKLY`.
- Audit action: `SERIES_BOARD_POLL_CLOSED`.
- Poll ID: `4F9EBAF8-1A20-4D77-8182-D9C064C35E80`.

## Recipient validation

- Expected active contributor count: `2`.
- Actual notification count: `2`.
- Distinct recipient count: `2`.
- Recipients: `TestEditor1` and `TestMangaka1`.
- No unrelated Board Chief, Board Member, Editor or Mangaka received the notification.
- No duplicate notification was created.

## Bell and read-state validation

- `TestEditor1` received one unread `Board Decision Approved` notification.
- Clicking it opened the exact Board Decision detail route.
- Notification ID: `9F83255A-254F-4647-E215-08DEE3365862`.
- `TestEditor1` notification became read.
- `TestMangaka1` notification remained unread.
- `test_editor_read_count = 1`.
- `test_mangaka_unread_count = 1`.
- `total_notification_count = 2`.

## Result

`BOARD_DECISION` implementation and runtime validation: PASS.
