USE MangaManagementDB;
GO
CREATE OR ALTER PROCEDURE audit.usp_AuditEvent_Append
    @actor_user_id      INT = NULL,
    @action_code        NVARCHAR(64),
    @entity_type        NVARCHAR(128),
    @entity_id          NVARCHAR(100) = NULL,
    @detail_json        NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @lock_result INT;
    DECLARE @actor_role_name NVARCHAR(128) = NULL;

    BEGIN TRY
        IF @detail_json IS NOT NULL AND ISJSON(@detail_json) <> 1
        BEGIN
            THROW 50001, 'detail_json must be valid JSON.', 1;
        END;

        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        /*
            Resolve actor role snapshot inside the audit procedure.
            This prevents the backend from passing role-name text manually.
        */
        IF @actor_user_id IS NOT NULL
        BEGIN
            SELECT
                @actor_role_name = r.role_name
            FROM auth.Users u
            INNER JOIN auth.Roles r
                ON r.role_id = u.role_id
            WHERE u.user_id = @actor_user_id;

            IF @actor_role_name IS NULL
            BEGIN
                THROW 50003, 'Actor user role could not be resolved.', 1;
            END;
        END;

        EXEC @lock_result = sys.sp_getapplock
            @Resource = N'audit_event_append',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lock_result < 0
        BEGIN
            THROW 50002, 'Could not acquire audit event append lock.', 1;
        END;

        INSERT INTO audit.AuditEvent
        (
            actor_user_id,
            actor_role_name,
            action_code,
            entity_type,
            entity_id,
            detail_json
        )
        VALUES
        (
            @actor_user_id,
            @actor_role_name,
            @action_code,
            @entity_type,
            @entity_id,
            @detail_json
        );

        IF @started_tran = 1
        BEGIN
            COMMIT;
        END;
    END TRY
    BEGIN CATCH
        IF @started_tran = 1 AND XACT_STATE() <> 0
        BEGIN
            ROLLBACK;
        END;

        THROW;
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE auth.usp_User_Create
    @role_name               NVARCHAR(30),
    @username                NVARCHAR(50),
    @email                   NVARCHAR(254),
    @password_hash           NVARCHAR(255),
    @display_name            NVARCHAR(100) = NULL,
    @avatar_file_id          BIGINT = NULL,
    @portfolio_file_id       BIGINT = NULL,

    @created_by_user_id      INT = NULL,

    @new_user_id             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @detail_json NVARCHAR(MAX);

    DECLARE @resolved_role_id SMALLINT;
    DECLARE @resolved_role_name NVARCHAR(30);

    DECLARE @normalized_role_name NVARCHAR(30) = LTRIM(RTRIM(@role_name));
    DECLARE @normalized_username NVARCHAR(50) = LTRIM(RTRIM(@username));
    DECLARE @normalized_email NVARCHAR(254) = LOWER(LTRIM(RTRIM(@email)));
    DECLARE @normalized_display_name NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@display_name)), N'');

    DECLARE @created_user TABLE
    (
        user_id     INT NOT NULL,
        status_code NVARCHAR(30) NOT NULL
    );

    BEGIN TRY
        IF @normalized_role_name IS NULL OR @normalized_role_name = N''
        BEGIN
            THROW 50010, 'Role name is required.', 1;
        END;

        IF @normalized_username IS NULL OR @normalized_username = N''
        BEGIN
            THROW 50012, 'Username is required.', 1;
        END;

        IF @normalized_email IS NULL OR @normalized_email = N''
        BEGIN
            THROW 50013, 'Email is required.', 1;
        END;

        IF @password_hash IS NULL OR LTRIM(RTRIM(@password_hash)) = N''
        BEGIN
            THROW 50014, 'Password hash is required.', 1;
        END;

        IF @normalized_display_name IS NULL
        BEGIN
            SET @normalized_display_name = @normalized_username;
        END;

        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        /*
            Resolve requested role name into role_id.
            App passes role name; database stores role_id.
        */
        SELECT
            @resolved_role_id = r.role_id,
            @resolved_role_name = r.role_name
        FROM auth.Roles r
        WHERE UPPER(LTRIM(RTRIM(r.role_name))) = UPPER(@normalized_role_name);

        IF @resolved_role_id IS NULL
        BEGIN
            THROW 50015, 'Invalid role name.', 1;
        END;

        INSERT INTO auth.Users
        (
            role_id,
            username,
            email,
            password_hash,
            display_name,
            avatar_file_id,
            portfolio_file_id,
            status_code
        )
        OUTPUT
            inserted.user_id,
            inserted.status_code
        INTO @created_user
        (
            user_id,
            status_code
        )
        VALUES
        (
            @resolved_role_id,
            @normalized_username,
            @normalized_email,
            @password_hash,
            @normalized_display_name,
            @avatar_file_id,
            @portfolio_file_id,
            N'PENDING_APPROVAL'
        );

        SELECT
            @new_user_id = user_id
        FROM @created_user;

        SELECT @detail_json =
        (
            SELECT
                @new_user_id AS user_id,
                @resolved_role_id AS role_id,
                @resolved_role_name AS role_name,
                @normalized_username AS username,
                @normalized_email AS email,
                @normalized_display_name AS display_name,
                N'PENDING_APPROVAL' AS status_code
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @created_by_user_id,
            @action_code = N'USER_REGISTERED',
            @entity_type = N'Users',
            @entity_id = @new_user_id,
            @detail_json = @detail_json;

        IF @started_tran = 1
        BEGIN
            COMMIT;
        END;
    END TRY
    BEGIN CATCH
        DECLARE @error_number INT = ERROR_NUMBER();

        IF @started_tran = 1 AND XACT_STATE() <> 0
        BEGIN
            ROLLBACK;
        END;

        IF @error_number IN (2601, 2627)
        BEGIN
            THROW 50011, 'Username or email already exists.', 1;
        END;

        THROW;
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE auth.usp_Admin_ChangeUserStatus
    @admin_user_id              INT,
    @admin_role_name_snapshot   NVARCHAR(128),

    @target_user_id             INT,
    @new_status_code            NVARCHAR(30),
    @reason                     NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @lock_result INT;

    DECLARE @old_status_code NVARCHAR(30);
    DECLARE @detail_json NVARCHAR(MAX);

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        /*
            Serialize status changes for the same target user.
            This avoids two admin actions changing the same account at the same time.
        */
        EXEC @lock_result = sys.sp_getapplock
            @Resource = CONCAT(N'auth_user_status_change_', @target_user_id),
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lock_result < 0
        BEGIN
            THROW 53101, 'Could not acquire user status change lock.', 1;
        END;

        /*
            Optional DB-side safety check.
            The UI should already restrict this to Admin users,
            but this prevents direct procedure misuse.
        */
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
            THROW 53102, 'Admin user is not active or does not have permission.', 1;
        END;

        /*
            Read current status with update lock before changing it.
        */
        SELECT
            @old_status_code = status_code
        FROM auth.Users WITH (UPDLOCK, HOLDLOCK)
        WHERE user_id = @target_user_id;

        IF @old_status_code IS NULL
        BEGIN
            THROW 53103, 'Target user does not exist.', 1;
        END;

        /*
            Do nothing if the requested status is already current.
            This keeps the procedure idempotent for repeated UI clicks.
        */
        IF @old_status_code = @new_status_code
        BEGIN
            IF @started_tran = 1
            BEGIN
                COMMIT;
            END;

            RETURN;
        END;

        UPDATE auth.Users
        SET
            status_code = @new_status_code,
            updated_at_utc = SYSUTCDATETIME()
        WHERE user_id = @target_user_id;

        SELECT @detail_json =
        (
            SELECT
                @target_user_id AS user_id,
                @old_status_code AS old_status_code,
                @new_status_code AS new_status_code,
                @reason AS reason
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @admin_user_id,
            @actor_role_name = @admin_role_name_snapshot,
            @action_code = N'USER_STATUS_CHANGED',
            @entity_type = N'Users',
            @entity_id = @target_user_id,
            @detail_json = @detail_json;

        IF @started_tran = 1
        BEGIN
            COMMIT;
        END;
    END TRY
    BEGIN CATCH
        IF @started_tran = 1 AND XACT_STATE() <> 0
        BEGIN
            ROLLBACK;
        END;

        THROW;
    END CATCH
END;
GO
GO
CREATE OR ALTER PROCEDURE auth.usp_User_ResetPassword
    @target_user_id      INT,
    @new_password_hash   NVARCHAR(255),
    @actor_user_id       INT = NULL,
    @actor_role_name     NVARCHAR(128) = NULL,
    @reset_mode          NVARCHAR(30),
    @reset_reason        NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

    DECLARE @old_status_code NVARCHAR(30);
    DECLARE @detail_json NVARCHAR(MAX);

    BEGIN TRY
        IF @reset_mode NOT IN (N'SELF_CHANGE', N'TOKEN_RESET', N'ADMIN_RESET')
        BEGIN
            THROW 52001, 'Invalid reset_mode.', 1;
        END;

        IF @new_password_hash IS NULL OR LEN(@new_password_hash) < 20
        BEGIN
            THROW 52002, 'new_password_hash is invalid.', 1;
        END;

        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        SELECT
            @old_status_code = status_code
        FROM auth.Users WITH (UPDLOCK, HOLDLOCK)
        WHERE user_id = @target_user_id;

        IF @old_status_code IS NULL
        BEGIN
            THROW 52003, 'Target user does not exist.', 1;
        END;

        /*
            Password reset updates only credential and login-failure state.

            It must not approve, reject, disable, or re-enable accounts.
            Therefore:
            - PENDING_APPROVAL stays PENDING_APPROVAL
            - ACTIVE stays ACTIVE
            - REJECTED stays REJECTED
            - DISABLED stays DISABLED

            Login permission is still decided by status_code elsewhere.
        */
        UPDATE auth.Users
        SET
            password_hash = @new_password_hash,
            failed_login_attempts = 0,
            last_failed_login_at = NULL,
            locked_until = NULL,
            updated_at_utc = @now
        WHERE user_id = @target_user_id;

        SELECT @detail_json =
        (
            SELECT
                @target_user_id AS user_id,
                @old_status_code AS status_code,
                @reset_mode AS reset_mode,
                @reset_reason AS reset_reason,
                N'Password hash updated and failed login state reset.' AS result
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @actor_user_id,
            @actor_role_name = @actor_role_name,
            @action_code =
                CASE
                    WHEN @reset_mode = N'SELF_CHANGE' THEN N'PASSWORD_CHANGED'
                    WHEN @reset_mode = N'TOKEN_RESET' THEN N'PASSWORD_RESET_BY_TOKEN'
                    WHEN @reset_mode = N'ADMIN_RESET' THEN N'PASSWORD_RESET_BY_ADMIN'
                END,
            @entity_type = N'Users',
            @entity_id = @target_user_id,
            @detail_json = @detail_json;

        IF @started_tran = 1
        BEGIN
            COMMIT;
        END;
    END TRY
    BEGIN CATCH
        IF @started_tran = 1 AND XACT_STATE() <> 0
        BEGIN
            ROLLBACK;
        END;

        THROW;
    END CATCH
END;
GO


