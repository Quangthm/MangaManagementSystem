USE MangaManagementDB;
GO

/* ============================================================
   MangaManagementSystem - Phase 7 Proc Test Script
   File: Proc_Test_Script.sql
   Scope: Auth/Admin stored-procedure persistence and safety-backstop checks
   Date: 2026-06-21

   IMPORTANT:
   - This script does not change C#, SQL schema, stored procedure definitions, or UI.
   - C# Application remains the business decision + orchestration layer.
   - SQL procedures are tested here only as persistence/audit/backstop boundaries.
   - The script creates uniquely named test users. Keep them for audit traceability or
     run against a disposable/local test database.
   ============================================================ */

SET NOCOUNT ON;

DECLARE @RunId NVARCHAR(32) = REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'');
DECLARE @PasswordHash NVARCHAR(255) =
    N'$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq';

DECLARE @ActiveAdminUserId UNIQUEIDENTIFIER;
DECLARE @ActiveMangakaUserId UNIQUEIDENTIFIER;
DECLARE @PendingAdminUserId UNIQUEIDENTIFIER;
DECLARE @PendingUserForRejectId UNIQUEIDENTIFIER;
DECLARE @DisabledUserId UNIQUEIDENTIFIER;

PRINT N'Phase 7 Proc Test Script started.';
PRINT N'RunId: ' + @RunId;

/* ------------------------------------------------------------
   0. Preflight: required roles must exist
   ------------------------------------------------------------ */

IF NOT EXISTS (SELECT 1 FROM auth.Roles WHERE role_name = N'Admin')
BEGIN
    ;THROW 59000, 'Missing required role: Admin.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM auth.Roles WHERE role_name = N'Mangaka')
BEGIN
    ;THROW 59001, 'Missing required role: Mangaka.', 1;
END;

/* ------------------------------------------------------------
   1. Create fixture users through auth.usp_User_Create
      Public/user create procedure must create PENDING_APPROVAL users.
   ------------------------------------------------------------ */

EXEC auth.usp_User_Create
    @role_name = N'Admin',
    @username = N'phase7_active_admin_' + @RunId,
    @email = N'phase7_active_admin_' + @RunId + N'@example.test',
    @password_hash = @PasswordHash,
    @display_name = N'Phase7 Active Admin',
    @created_by_user_id = NULL,
    @new_user_id = @ActiveAdminUserId OUTPUT;

UPDATE auth.Users
SET status_code = N'ACTIVE'
WHERE user_id = @ActiveAdminUserId;

EXEC auth.usp_User_Create
    @role_name = N'Mangaka',
    @username = N'phase7_active_mangaka_' + @RunId,
    @email = N'phase7_active_mangaka_' + @RunId + N'@example.test',
    @password_hash = @PasswordHash,
    @display_name = N'Phase7 Active Mangaka',
    @created_by_user_id = NULL,
    @new_user_id = @ActiveMangakaUserId OUTPUT;

UPDATE auth.Users
SET status_code = N'ACTIVE'
WHERE user_id = @ActiveMangakaUserId;

EXEC auth.usp_User_Create
    @role_name = N'Admin',
    @username = N'phase7_pending_admin_' + @RunId,
    @email = N'phase7_pending_admin_' + @RunId + N'@example.test',
    @password_hash = @PasswordHash,
    @display_name = N'Phase7 Pending Admin',
    @created_by_user_id = NULL,
    @new_user_id = @PendingAdminUserId OUTPUT;

IF NOT EXISTS
(
    SELECT 1
    FROM auth.Users u
    INNER JOIN auth.Roles r
        ON r.role_id = u.role_id
    WHERE u.user_id = @PendingAdminUserId
      AND r.role_name = N'Admin'
      AND u.status_code = N'PENDING_APPROVAL'
)
BEGIN
    ;THROW 59002, 'FAIL: Public Admin user was not created as PENDING_APPROVAL.', 1;
END;

PRINT N'PASS 1: auth.usp_User_Create creates public Admin request as PENDING_APPROVAL, not ACTIVE.';

/* ------------------------------------------------------------
   2. Non-Admin cannot approve/activate pending Admin through Admin proc
   ------------------------------------------------------------ */

BEGIN TRY
    EXEC auth.usp_Admin_ChangeUserStatus
        @admin_user_id = @ActiveMangakaUserId,
        @target_user_id = @PendingAdminUserId,
        @new_status_code = N'ACTIVE',
        @reason = NULL;

    ;THROW 59003, 'FAIL: Non-Admin actor was able to activate a pending Admin account.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 59003
    BEGIN
        ;THROW;
    END;

    PRINT N'PASS 2: Non-Admin actor cannot activate pending Admin account.';
END CATCH;

/* ------------------------------------------------------------
   3. Active Admin can approve pending Admin
   ------------------------------------------------------------ */

EXEC auth.usp_Admin_ChangeUserStatus
    @admin_user_id = @ActiveAdminUserId,
    @target_user_id = @PendingAdminUserId,
    @new_status_code = N'ACTIVE',
    @reason = NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM auth.Users
    WHERE user_id = @PendingAdminUserId
      AND status_code = N'ACTIVE'
)
BEGIN
    ;THROW 59004, 'FAIL: Active Admin could not approve pending Admin account.', 1;
END;

PRINT N'PASS 3: Active Admin can approve pending Admin account.';

/* ------------------------------------------------------------
   4. Reject requires a non-empty reason
   ------------------------------------------------------------ */

EXEC auth.usp_User_Create
    @role_name = N'Mangaka',
    @username = N'phase7_reject_target_' + @RunId,
    @email = N'phase7_reject_target_' + @RunId + N'@example.test',
    @password_hash = @PasswordHash,
    @display_name = N'Phase7 Reject Target',
    @created_by_user_id = NULL,
    @new_user_id = @PendingUserForRejectId OUTPUT;

BEGIN TRY
    EXEC auth.usp_Admin_ChangeUserStatus
        @admin_user_id = @ActiveAdminUserId,
        @target_user_id = @PendingUserForRejectId,
        @new_status_code = N'REJECTED',
        @reason = NULL;

    ;THROW 59005, 'FAIL: Reject without reason was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 59005
    BEGIN
        ;THROW;
    END;

    PRINT N'PASS 4: Reject without reason is blocked.';
END CATCH;

EXEC auth.usp_Admin_ChangeUserStatus
    @admin_user_id = @ActiveAdminUserId,
    @target_user_id = @PendingUserForRejectId,
    @new_status_code = N'REJECTED',
    @reason = N'Phase 7 rejected-status test.';

IF NOT EXISTS
(
    SELECT 1
    FROM auth.Users
    WHERE user_id = @PendingUserForRejectId
      AND status_code = N'REJECTED'
)
BEGIN
    ;THROW 59006, 'FAIL: User was not changed to REJECTED after valid reject reason.', 1;
END;

PRINT N'PASS 5: Reject with reason sets status_code = REJECTED.';

/* ------------------------------------------------------------
   5. Disabled transition requires reason and can be reactivated by Admin
   ------------------------------------------------------------ */

EXEC auth.usp_User_Create
    @role_name = N'Mangaka',
    @username = N'phase7_disable_target_' + @RunId,
    @email = N'phase7_disable_target_' + @RunId + N'@example.test',
    @password_hash = @PasswordHash,
    @display_name = N'Phase7 Disable Target',
    @created_by_user_id = NULL,
    @new_user_id = @DisabledUserId OUTPUT;

EXEC auth.usp_Admin_ChangeUserStatus
    @admin_user_id = @ActiveAdminUserId,
    @target_user_id = @DisabledUserId,
    @new_status_code = N'ACTIVE',
    @reason = NULL;

EXEC auth.usp_Admin_ChangeUserStatus
    @admin_user_id = @ActiveAdminUserId,
    @target_user_id = @DisabledUserId,
    @new_status_code = N'DISABLED',
    @reason = N'Phase 7 disable test.';

IF NOT EXISTS
(
    SELECT 1
    FROM auth.Users
    WHERE user_id = @DisabledUserId
      AND status_code = N'DISABLED'
)
BEGIN
    ;THROW 59007, 'FAIL: User was not changed to DISABLED.', 1;
END;

EXEC auth.usp_Admin_ChangeUserStatus
    @admin_user_id = @ActiveAdminUserId,
    @target_user_id = @DisabledUserId,
    @new_status_code = N'ACTIVE',
    @reason = NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM auth.Users
    WHERE user_id = @DisabledUserId
      AND status_code = N'ACTIVE'
)
BEGIN
    ;THROW 59008, 'FAIL: Disabled user was not reactivated by active Admin.', 1;
END;

PRINT N'PASS 6: Disable and reactivate workflow works through Admin status procedure.';

/* ------------------------------------------------------------
   6. Admin self-disable/reject is blocked
   ------------------------------------------------------------ */

BEGIN TRY
    EXEC auth.usp_Admin_ChangeUserStatus
        @admin_user_id = @ActiveAdminUserId,
        @target_user_id = @ActiveAdminUserId,
        @new_status_code = N'DISABLED',
        @reason = N'Phase 7 self-disable test.';

    ;THROW 59009, 'FAIL: Admin was able to disable own account.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 59009
    BEGIN
        ;THROW;
    END;

    PRINT N'PASS 7: Admin self-disable/reject protection is enforced.';
END CATCH;

/* ------------------------------------------------------------
   7. Audit evidence
   ------------------------------------------------------------ */

SELECT TOP (50)
    ae.occurred_at_utc,
    ae.actor_user_id,
    ae.actor_role_name,
    ae.action_code,
    ae.entity_type,
    ae.entity_id,
    ae.detail_json
FROM audit.AuditEvent ae
WHERE ae.entity_id IN
(
    CONVERT(NVARCHAR(36), @PendingAdminUserId),
    CONVERT(NVARCHAR(36), @PendingUserForRejectId),
    CONVERT(NVARCHAR(36), @DisabledUserId)
)
ORDER BY ae.occurred_at_utc DESC;

PRINT N'Phase 7 Proc Test Script completed successfully.';
GO
