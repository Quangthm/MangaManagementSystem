USE [MangaManagementDB]
GO

CREATE OR ALTER PROCEDURE auth.usp_Admin_ChangeUserStatus
    @admin_user_id              UNIQUEIDENTIFIER,
    @target_user_id             UNIQUEIDENTIFIER,
    @new_status_code            NVARCHAR(30),
    @reason                     NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @lock_result INT;
    DECLARE @lock_resource NVARCHAR(255);

    DECLARE @old_status_code NVARCHAR(30);
    DECLARE @normalized_new_status_code NVARCHAR(30) =
        UPPER(LTRIM(RTRIM(@new_status_code)));
    DECLARE @normalized_reason NVARCHAR(500) =
        NULLIF(LTRIM(RTRIM(@reason)), N'');

    DECLARE @audit_entity_id NVARCHAR(100);
    DECLARE @detail_json NVARCHAR(MAX);

    BEGIN TRY
        IF @admin_user_id IS NULL
        BEGIN
            ;THROW 53100, 'Admin user id is required.', 1;
        END;

        IF @target_user_id IS NULL
        BEGIN
            ;THROW 53104, 'Target user id is required.', 1;
        END;

        IF @normalized_new_status_code NOT IN
        (
            N'PENDING_APPROVAL',
            N'ACTIVE',
            N'REJECTED',
            N'DISABLED'
        )
        BEGIN
            ;THROW 53105, 'Unsupported target user status.', 1;
        END;

        IF @normalized_new_status_code IN
        (
            N'REJECTED',
            N'DISABLED'
        )
        AND @normalized_reason IS NULL
        BEGIN
            ;THROW 53106, 'A reason is required when rejecting or disabling an account.', 1;
        END;

        IF LEN(@normalized_reason) > 500
        BEGIN
            ;THROW 53107, 'Reason cannot exceed 500 characters.', 1;
        END;

        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        SET @lock_resource =
            N'auth_user_status_change_'
            + CONVERT(NVARCHAR(36), @target_user_id);

        EXEC @lock_result = sys.sp_getapplock
            @Resource = @lock_resource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lock_result < 0
        BEGIN
            ;THROW 53101, 'Could not acquire user status change lock.', 1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM auth.Users u
            INNER JOIN auth.Roles r
                ON r.role_id = u.role_id
            WHERE u.user_id = @admin_user_id
              AND u.status_code = N'ACTIVE'
              AND r.role_name = N'Admin'
        )
        BEGIN
            ;THROW 53102, 'Admin user is not active or does not have permission.', 1;
        END;

        SELECT
            @old_status_code = u.status_code
        FROM auth.Users u WITH (UPDLOCK, HOLDLOCK)
        WHERE u.user_id = @target_user_id;

        IF @old_status_code IS NULL
        BEGIN
            ;THROW 53103, 'Target user does not exist.', 1;
        END;

        IF @admin_user_id = @target_user_id
           AND @normalized_new_status_code <> N'ACTIVE'
        BEGIN
            ;THROW 53108, 'An administrator cannot reject or disable their own account.', 1;
        END;

        IF @old_status_code = @normalized_new_status_code
        BEGIN
            IF @started_tran = 1
            BEGIN
                COMMIT;
            END;

            RETURN;
        END;

        IF NOT
        (
            (@old_status_code = N'PENDING_APPROVAL'
                AND @normalized_new_status_code IN
                    (N'ACTIVE', N'REJECTED', N'DISABLED'))
            OR
            (@old_status_code = N'REJECTED'
                AND @normalized_new_status_code = N'ACTIVE')
            OR
            (@old_status_code = N'DISABLED'
                AND @normalized_new_status_code = N'ACTIVE')
            OR
            (@old_status_code = N'ACTIVE'
                AND @normalized_new_status_code = N'DISABLED')
        )
        BEGIN
            ;THROW 53109, 'The requested account status transition is not allowed.', 1;
        END;

        UPDATE auth.Users
        SET
            status_code = @normalized_new_status_code
        WHERE user_id = @target_user_id;

        SELECT @detail_json =
        (
            SELECT
                @target_user_id AS user_id,
                @old_status_code AS old_status_code,
                @normalized_new_status_code AS new_status_code,
                @normalized_reason AS reason
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        SET @audit_entity_id =
            CONVERT(NVARCHAR(36), @target_user_id);

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @admin_user_id,
            @action_code = N'USER_STATUS_CHANGED',
            @entity_type = N'Users',
            @entity_id = @audit_entity_id,
            @detail_json = @detail_json;

        IF @started_tran = 1
        BEGIN
            COMMIT;
        END;
    END TRY
    BEGIN CATCH
        IF @started_tran = 1
           AND XACT_STATE() <> 0
        BEGIN
            ROLLBACK;
        END;

        ;THROW;
    END CATCH
END;
GO
