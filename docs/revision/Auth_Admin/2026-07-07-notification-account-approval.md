# 2026-07-07 - Notification on Account Approval

## Task
Implement the first notification step requested by the leader:
- Insert a notification into the database when an admin approves a user account.
- Send an email to the user when their account is approved.

## Scope
Included:
- Account approval flow only.
- Database notification insert through the existing NotificationService.
- Account approved email through the existing EmailService infrastructure.

Excluded:
- Realtime notification.
- SignalR.
- Notification bell UI.
- Unread count.
- Notification center.
- Reject, disable, and activate flows.
- Database schema changes.
- Migration changes.
- Stored procedure changes.

## Files Changed

### MangaManagementSystem/src/MangaManagementSystem.Application/Interfaces/IEmailService.cs
Added SendAccountApprovedEmailAsync(...) to the existing email service contract.

### MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Services/EmailService.cs
Implemented SendAccountApprovedEmailAsync(...) by reusing the existing private SendEmailAsync(...) helper.

### MangaManagementSystem/src/MangaManagementSystem.Application/Services/UserService.cs
Updated ApproveUserAsync(...) so that after the existing account status update succeeds:
1. User status is changed to ACTIVE by the existing ChangeUserStatusViaProcAsync(...) flow.
2. A notification is inserted through INotificationService.CreateNotificationAsync(...).
3. An account approved email is sent through IEmailService.SendAccountApprovedEmailAsync(...).
4. The updated UserDto is returned as before.

Notification inserted:
- NotificationTypeCode: SYSTEM_MESSAGE
- Title: Account approved
- Message: Your MangaFlow account has been approved. You can now sign in and start using your account.
- RelatedEntityType: User
- RelatedEntityId: approved user id

## Design Notes
The notification and email logic was placed in UserService.ApproveUserAsync(...) because this Application service method owns the account approval business flow.

The API controller still only dispatches ApproveAdminUserCommand.
The command handler still delegates to IUserService.
The existing database status change procedure remains unchanged.

No database schema, migration, or stored procedure was changed because the project already had:
- Notification entity.
- NotificationDto and CreateNotificationDto.
- INotificationService and NotificationService.
- manga.Notification EF configuration.
- IUnitOfWork.Notifications.

## Verification

Commands run:
- git diff --check
- dotnet build C:\jira\MangaManagementSystem\MangaManagementSystem.slnx
- git grep -n "SYSTEM_MESSAGE\|SendAccountApprovedEmailAsync\|Account approved" -- MangaManagementSystem/src
- git status --short
- git diff --name-status

Build result:
- Build succeeded with 62 warning(s) in 16.8s.
- No build errors were introduced by this task.
- Existing warnings remain in Infrastructure/Web/generated Razor/MudBlazor analyzer areas.

Grep evidence:
- IEmailService.cs contains SendAccountApprovedEmailAsync.
- UserService.cs contains Account approved.
- UserService.cs contains SYSTEM_MESSAGE.
- UserService.cs calls SendAccountApprovedEmailAsync.
- EmailService.cs implements SendAccountApprovedEmailAsync.

## Manual Test Suggestion
1. Run API/Web.
2. Login as admin.
3. Open Admin User Management / User Approval page.
4. Approve a user with status PENDING_APPROVAL or approve again from REJECTED.
5. Verify:
   - User status becomes ACTIVE.
   - A row is inserted into manga.Notification with NotificationTypeCode = SYSTEM_MESSAGE and title/message for account approval.
   - If SMTP mock is enabled, logs show mock email for account approval.
   - If SMTP real config is enabled, the approved user receives the email.

## Current Git Notes
Do not add or commit these existing temporary untracked files:
- leader_followup_audit.txt
- leader_followup_token_exact.txt
- leader_followup_ui_auth_audit.txt
- tem

## Leader Report Short Version
Implemented the first notification step for account approval. When an admin approves a pending/rejected user account, the existing approval flow now inserts an SYSTEM_MESSAGE notification through NotificationService and sends an account-approved email through the existing EmailService. No database schema, migration, stored procedure, realtime notification, SignalR, notification bell, unread count, or notification center changes were made. Build passed with existing warnings.

## Manual Test Evidence

Manual browser/DB/API-log test was completed after fixing the notification type code to match the existing database CHECK constraint.

Test account:

- Username: TestNotifi2
- Email: gamesteamphan+test2@gmail.com
- Role: Mangaka

Observed result:

- Admin approved the pending account from the User Approval page.
- UI showed a success toast: `Approved TestNotifi2`.
- User Approval page then showed no pending users.
- User Management showed the account as `ACTIVE`.
- `manga.Notification` contained a new row with:
  - `notification_type_code = SYSTEM_MESSAGE`
  - `title = Account approved`
  - `message = Your MangaFlow account has been approved...`
  - `related_entity_type = User`
  - `related_entity_id = approved user id`
- API log showed an insert into `[manga].[Notification]`.
- API log showed SMTP email sent to `gamesteamphan+test2@gmail.com` with subject `Your MangaFlow account has been approved`.

Runtime issue found and resolved:

- Initial implementation used `ACCOUNT_APPROVED`.
- Manual test showed SQL CHECK constraint `ck_notification_type_code` rejected that value.
- Code was adjusted to use existing valid type `SYSTEM_MESSAGE`.
- No database schema, migration, stored procedure, or CHECK constraint was changed.
