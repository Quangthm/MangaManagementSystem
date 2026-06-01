USE MangaManagementDB;
GO
CREATE OR ALTER PROCEDURE audit.usp_AuditEvent_Append
    @actor_user_id      INT = NULL,
    @actor_role_name    NVARCHAR(128) = NULL,
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

    BEGIN TRY
        /*
            Keep JSON validation because detail_json is optional,
            but when provided it should be valid JSON.
            Required fields such as action_code and entity_type are
            handled by database constraints and application logic.
        */
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
            Serialize audit appends under high concurrency.
            With @LockOwner = 'Transaction', SQL Server releases the
            application lock when the transaction commits or rolls back.
        */
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
CREATE OR ALTER PROCEDURE dbo.User_UpdateStatus
    @target_user_id        INT,
    @new_status            NVARCHAR(20),
    @changed_by_user_id    INT,
    @changed_by_role_name  NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @audit_id INT, @chain_hash CHAR(64);
    DECLARE @old_status NVARCHAR(20);
    DECLARE @normalized_status NVARCHAR(20);
    DECLARE @diff_json NVARCHAR(MAX);

    BEGIN TRY
        BEGIN TRAN;

        /* -----------------------------
           0) Normalize input
           ----------------------------- */
        SET @normalized_status = UPPER(LTRIM(RTRIM(@new_status)));

        /* -----------------------------
           1) Load current status + validate target exists
           ----------------------------- */
        SELECT @old_status = u.status
        FROM dbo.Users u
        WHERE u.user_id = @target_user_id;

        IF @old_status IS NULL
            THROW 53001, 'Target user not found.', 1;

        /* -----------------------------
           2) Block changes for ADMIN targets (active admin role)
           ----------------------------- */
        IF EXISTS (
            SELECT 1
            FROM dbo.UserRole ur
            JOIN dbo.Roles r ON r.role_id = ur.role_id
            WHERE ur.user_id = @target_user_id
              AND ur.revoked_at IS NULL
              AND r.role_name = 'ADMIN'
        )
            THROW 53002, 'Cannot change status for an ADMIN account.', 1;

        /* -----------------------------
           3) No-op guard (optional but clean)
           ----------------------------- */
        IF @old_status = @normalized_status
            THROW 53003, 'User already has the requested status.', 1;

        /* -----------------------------
           4) Update
           - CHECK constraint on Users.status enforces allowed values
           ----------------------------- */
        UPDATE dbo.Users
        SET status = @normalized_status
        WHERE user_id = @target_user_id;

        IF @@ROWCOUNT <> 1
            THROW 53004, 'Status update failed unexpectedly.', 1;

        /* -----------------------------
           5) Audit + hash chain
           ----------------------------- */
        SELECT @diff_json =
        (
            SELECT
                @target_user_id      AS target_user_id,
                @old_status          AS old_status,
                @normalized_status   AS new_status
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC dbo.Audit_Append
            @actor_user_id   = @changed_by_user_id,
            @actor_role_name = @changed_by_role_name,
            @action          = N'CHANGE_USER_STATUS',
            @object_type     = N'Users',
            @object_id       = @target_user_id,
            @diff_json       = @diff_json,
            @audit_id        = @audit_id OUTPUT,
            @chain_hash      = @chain_hash OUTPUT;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;
        THROW;
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE auth.usp_User_Create
    @role_id                 SMALLINT,
    @username                NVARCHAR(50),
    @email                   NVARCHAR(254),
    @password_hash           NVARCHAR(255),
    @avatar_file_id          BIGINT = NULL,
    @portfolio_file_id       BIGINT = NULL,

    @created_by_user_id      INT = NULL,
    @created_by_role_name    NVARCHAR(128) = NULL,

    @new_user_id             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @detail_json NVARCHAR(MAX);

    DECLARE @created_user TABLE
    (
        user_id     INT NOT NULL,
        status_code NVARCHAR(30) NOT NULL
    );

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        INSERT INTO auth.Users
        (
            role_id,
            username,
            email,
            password_hash,
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
            @role_id,
            LTRIM(RTRIM(@username)),
            LTRIM(RTRIM(@email)),
            @password_hash,
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
                @role_id AS role_id,
                LTRIM(RTRIM(@username)) AS username,
                LTRIM(RTRIM(@email)) AS email,
                N'PENDING_APPROVAL' AS status_code
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @created_by_user_id,
            @actor_role_name = @created_by_role_name,
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
CREATE OR ALTER PROCEDURE dbo.RolePerm_Grant_Batch
    @role_name            NVARCHAR(30),
    @perm_list_json       NVARCHAR(MAX),   -- JSON array: [{"module":"X","action":"READ"}, ...]
    @granted_by_user_id   INT,
    @granted_by_role_name NVARCHAR(30),
    @audit_action         NVARCHAR(80) = N'GRANT_ROLEPERM_BULK'  -- e.g., BOOTSTRAP_SEED_ROLEPERM_TECHNICIAN
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @r NVARCHAR(50) = UPPER(LTRIM(RTRIM(@role_name)));
    DECLARE @role_id INT;

    DECLARE @audit_id INT, @chain_hash CHAR(64);
    DECLARE @diff_json NVARCHAR(MAX);

    BEGIN TRY
        BEGIN TRAN;

        /* 1) Resolve role_id */
        SELECT @role_id = r.role_id
        FROM dbo.Roles r
        WHERE r.role_name = @r;

        IF @role_id IS NULL
            THROW 50101, 'Role not found.', 1;

        /* 2) Parse JSON perm list into a table variable (normalize identifiers) */
        DECLARE @wanted TABLE (
            module NVARCHAR(50) NOT NULL,
            action NVARCHAR(10) NOT NULL,
            PRIMARY KEY (module, action)
        );

        INSERT INTO @wanted(module, action)
        SELECT
            UPPER(LTRIM(RTRIM(j.[module]))) AS module,
            UPPER(LTRIM(RTRIM(j.[action]))) AS action
        FROM OPENJSON(@perm_list_json)
        WITH (
            [module] NVARCHAR(50) '$.module',
            [action] NVARCHAR(10) '$.action'
        ) AS j;

        /* 3) Insert missing RolePerm rows (idempotent) */
        DECLARE @inserted TABLE (permission_id INT);

        INSERT INTO dbo.RolePerm (role_id, permission_id, granted_by_user_id)
        OUTPUT inserted.permission_id INTO @inserted(permission_id)
        SELECT
            @role_id,
            p.permission_id,
            @granted_by_user_id
        FROM @wanted w
        JOIN dbo.Permissions p
          ON p.module = w.module
         AND p.action = w.action
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.RolePerm rp
            WHERE rp.role_id = @role_id
              AND rp.permission_id = p.permission_id
        );

        DECLARE @inserted_count INT = (SELECT COUNT(*) FROM @inserted);


        SELECT @diff_json =
        (
            SELECT
                @r AS role,
                @granted_by_user_id AS granted_by_user_id,
                @inserted_count AS inserted_count,
                JSON_QUERY(@perm_list_json) AS requested_permissions
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        /* 5) Audit as a single semantic event */
        EXEC dbo.Audit_Append
            @actor_user_id   = @granted_by_user_id,
            @actor_role_name = @granted_by_role_name,
            @action          = @audit_action,
            @object_type     = N'RolePerm',
            @object_id       = @role_id,
            @diff_json       = @diff_json,
            @audit_id        = @audit_id OUTPUT,
            @chain_hash      = @chain_hash OUTPUT;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;
        THROW;
    END CATCH
END;
GO
GO
CREATE OR ALTER PROCEDURE auth.User_ResetPassword
    @target_user_id INT,
    @new_password_hash NVARCHAR(255),
    @actor_user_id INT = NULL,
    @actor_role_name NVARCHAR(128) = NULL,
    @reset_mode NVARCHAR(30),
    @reset_reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

    DECLARE @old_status NVARCHAR(30);
    DECLARE @new_status NVARCHAR(30);

    DECLARE @audit_event_id BIGINT;
    DECLARE @chain_hash CHAR(64);

    IF @reset_mode NOT IN (N'SELF_CHANGE', N'TOKEN_RESET', N'ADMIN_RESET')
    BEGIN
        THROW 52001, 'Invalid reset_mode.', 1;
    END;

    IF @new_password_hash IS NULL OR LEN(@new_password_hash) < 20
    BEGIN
        THROW 52002, 'new_password_hash is invalid.', 1;
    END;

    BEGIN TRY
        BEGIN TRAN;

        SELECT @old_status = status
        FROM auth.Users WITH (UPDLOCK, HOLDLOCK)
        WHERE user_id = @target_user_id;

        IF @old_status IS NULL
        BEGIN
            THROW 52003, 'Target user does not exist.', 1;
        END;

        /*
            Password reset clears login failure state.
            It only turns LOCKED back to ACTIVE.
            It does not activate DISABLED or PENDING_APPROVAL accounts.
        */
        SET @new_status =
            CASE
                WHEN @old_status = 'LOCKED' THEN 'ACTIVE'
                ELSE @old_status
            END;

        UPDATE auth.Users
        SET
            password_hash = @new_password_hash,
            status = @new_status,
            failed_login_attempts = 0,
            last_failed_login_at = NULL,
            locked_until = NULL,
            updated_at = @now
        WHERE user_id = @target_user_id;

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @actor_user_id,
            @actor_role_name = @actor_role_name,
            @action_code =
                CASE
                    WHEN @reset_mode = N'SELF_CHANGE' THEN N'PASSWORD_CHANGED'
                    WHEN @reset_mode = N'TOKEN_RESET' THEN N'PASSWORD_RESET_BY_TOKEN'
                    ELSE N'PASSWORD_RESET_BY_ADMIN'
                END,
            @entity_type = N'User',
            @entity_id = @target_user_id,
            @detail_json = N'{"result":"Password hash updated and failed login state reset."}',
            @audit_event_id = @audit_event_id OUTPUT,
            @chain_hash = @chain_hash OUTPUT;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;

        THROW;
    END CATCH
END;
GO
GO
CREATE OR ALTER PROCEDURE dbo.Bootstrap_Init
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @audit_id INT, @chain_hash CHAR(64);
    DECLARE @admin_id INT;
    DECLARE @new_device_id INT;
    DECLARE @new_profile_id INT;
    DECLARE @new_calib_id INT;
    BEGIN TRY
        BEGIN TRAN;

        /* ===== Run-once sentinel guard (no tracking table) ===== */
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE user_id = 2 AND username = N'SYSTEM')
            THROW 50150, 'Bootstrap already applied (SYSTEM user exists).', 1;

        /* =========================================================
           1) Deterministic actors: anonymous=1, SYSTEM=2
           ========================================================= */
        SET IDENTITY_INSERT dbo.Users ON;

        INSERT INTO dbo.Users (user_id, username, password_hash, status)
        VALUES
            (1, N'anonymous',
             CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), N'ANONYMOUS_DISABLED')), 2),
             N'ACTIVE'),
            (2, N'SYSTEM',
             CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), N'SYSTEM_DISABLED')), 2),
             N'ACTIVE');

        SET IDENTITY_INSERT dbo.Users OFF;

        DECLARE @actors_diff NVARCHAR(MAX) =
        (
            SELECT 1 AS anonymous_id, 2 AS system_id
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC dbo.Audit_Append
            @actor_user_id   = 2,
            @actor_role_name = N'SYSTEM',
            @action          = N'BOOTSTRAP_CREATE_SYSTEM_ACTORS',
            @object_type     = N'Users',
            @object_id       = 0,
            @diff_json       = @actors_diff,
            @audit_id        = @audit_id OUTPUT,
            @chain_hash      = @chain_hash OUTPUT;

        /* =========================================================
           2) Seed Roles (non-idempotent insert)
           ========================================================= */
        INSERT INTO dbo.Roles(role_name)
        VALUES (N'ADMIN'), (N'OPERATOR'), (N'TECHNICIAN'), (N'AUDITOR'), (N'GUEST');
        DECLARE @roles_diff NVARCHAR(MAX) =
        (
                SELECT role_name
                FROM (VALUES (N'ADMIN'), (N'OPERATOR'), (N'TECHNICIAN'), (N'AUDITOR'), (N'GUEST')) v(role_name)
                ORDER BY role_name
                FOR JSON PATH
        );
        EXEC dbo.Audit_Append 
            @actor_user_id   = 2,
            @actor_role_name = N'SYSTEM',
            @action          = N'BOOTSTRAP_SEED_ROLES',
            @object_type     = N'Roles',
            @object_id       = 0,
            @diff_json       = @roles_diff,
            @audit_id        = @audit_id OUTPUT,
            @chain_hash      = @chain_hash OUTPUT;

        /* =========================================================
           3) Create admin user via User_Create (BOOTSTRAP action)
           ========================================================= */
        DECLARE @admin_pw_hash NVARCHAR(255) =
        CONVERT(NVARCHAR(255),
            CONVERT(VARCHAR(64),
                    HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), N'admin123')),
                    2));
        EXEC dbo.User_Create
            @username             = N'admin',
            @password_hash        = @admin_pw_hash,
            @status               = N'ACTIVE',
            @created_by_user_id   = 2,
            @created_by_role_name = N'SYSTEM',
            @new_user_id          = @admin_id OUTPUT,
            @audit_action         = N'BOOTSTRAP_CREATE_ADMIN_USER';

        /* =========================================================
           4) Seed Permissions (non-idempotent insert)
           ========================================================= */
        INSERT INTO dbo.Permissions(module, action)
        VALUES
            (N'USER',N'READ'),(N'USER',N'WRITE'),
            (N'ROLE',N'READ'),(N'ROLE',N'WRITE'),
            (N'DEVICE',N'READ'),(N'DEVICE',N'WRITE'),
            (N'POUR_PROFILE',N'READ'),(N'POUR_PROFILE',N'WRITE'),
            (N'POUR_SESSION',N'READ'),(N'POUR_SESSION',N'WRITE'),
            (N'POUR_HISTORY',N'READ'),
            (N'POUR_CURATE', N'READ'),
            (N'POUR_CURATE', N'WRITE'),
            (N'ALERT',N'READ'),(N'ALERT',N'WRITE'),
            (N'ALERT_EVIDENCE',N'READ'),(N'ALERT_EVIDENCE',N'WRITE'),
            (N'ALERT_REASON',N'READ'),
            (N'MAINTENANCE',N'READ'),(N'MAINTENANCE',N'WRITE'),
            (N'CALIBRATION',N'READ'),(N'CALIBRATION',N'WRITE'),
            (N'AUDIT',N'READ'),
            (N'HASHCHAIN',N'READ'),
            (N'SENSOR_LOG', N'READ');
        DECLARE @perms_diff NVARCHAR(MAX) =
            (
                SELECT module, action
                FROM dbo.Permissions
                ORDER BY module, action
                FOR JSON PATH
            );
        EXEC dbo.Audit_Append
            @actor_user_id   = 2,
            @actor_role_name = N'SYSTEM',
            @action          = N'BOOTSTRAP_SEED_PERMISSIONS',
            @object_type     = N'Permissions',
            @object_id       = 0,
            @diff_json       = @perms_diff,
            @audit_id        = @audit_id OUTPUT,
            @chain_hash      = @chain_hash OUTPUT;

        /* =========================================================
           5) Seed RolePerm via sp_RolePerm_Grant_Batch (BOOTSTRAP actions)
           ========================================================= */

        DECLARE @admin_perms_json NVARCHAR(MAX) =
        (
            SELECT module, action
            FROM dbo.Permissions
            ORDER BY module, action
            FOR JSON PATH
        );

        EXEC dbo.RolePerm_Grant_Batch
            @role_name = N'ADMIN',
            @perm_list_json = @admin_perms_json,
            @granted_by_user_id = 2,
            @granted_by_role_name = N'SYSTEM',
            @audit_action = N'BOOTSTRAP_SEED_ROLEPERM_ADMIN';

        DECLARE @tech_perms_json NVARCHAR(MAX) =
        (
            SELECT module, action
            FROM dbo.Permissions
            WHERE (module = N'DEVICE' AND action IN (N'READ',N'WRITE'))
               OR (module = N'CALIBRATION' AND action IN (N'READ',N'WRITE'))
               OR (module = N'MAINTENANCE' AND action IN (N'READ',N'WRITE'))
               OR (module = N'ALERT' AND action IN (N'READ',N'WRITE'))
               OR (module = N'ALERT_EVIDENCE' AND action IN (N'READ',N'WRITE'))
               OR (module = N'POUR_CURATE' AND action IN (N'READ',N'WRITE'))
               OR (module = N'ALERT_REASON' AND action = N'READ')
               OR (module = N'POUR_PROFILE' AND action = N'READ')
               OR (module = N'POUR_HISTORY' AND action = N'READ')
               OR (module = N'SENSOR_LOG' AND action = N'READ')
            ORDER BY module, action
            FOR JSON PATH
        );

        EXEC dbo.RolePerm_Grant_Batch
            @role_name = N'TECHNICIAN',
            @perm_list_json = @tech_perms_json,
            @granted_by_user_id = 2,
            @granted_by_role_name = N'SYSTEM',
            @audit_action = N'BOOTSTRAP_SEED_ROLEPERM_TECHNICIAN';

        DECLARE @op_perms_json NVARCHAR(MAX) =
        (
            SELECT module, action
            FROM dbo.Permissions
            WHERE (module = N'POUR_SESSION' AND action IN (N'READ',N'WRITE'))
               OR (module = N'POUR_PROFILE' AND action = N'READ')
               OR (module = N'DEVICE' AND action = N'READ')
               OR (module = N'ALERT' AND action = N'READ')
            ORDER BY module, action
            FOR JSON PATH
        );

        EXEC dbo.RolePerm_Grant_Batch
            @role_name = N'OPERATOR',
            @perm_list_json = @op_perms_json,
            @granted_by_user_id = 2,
            @granted_by_role_name = N'SYSTEM',
            @audit_action = N'BOOTSTRAP_SEED_ROLEPERM_OPERATOR';

        DECLARE @auditor_perms_json NVARCHAR(MAX) =
        (
            SELECT module, action
            FROM dbo.Permissions
            WHERE (module = N'POUR_SESSION' AND action = N'READ')
               OR (module = N'POUR_HISTORY' AND action = N'READ')
               OR (module = N'POUR_CURATE' AND action = N'READ')
               OR (module = N'ALERT' AND action = N'READ')
               OR (module = N'ALERT_REASON' AND action = N'READ')
               OR (module = N'ALERT_EVIDENCE' AND action = N'READ')
               OR (module = N'AUDIT' AND action = N'READ')
               OR (module = N'HASHCHAIN' AND action = N'READ')
               OR (module = N'SENSOR_LOG' AND action = N'READ')

            ORDER BY module, action
            FOR JSON PATH
        );

        EXEC dbo.RolePerm_Grant_Batch
            @role_name = N'AUDITOR',
            @perm_list_json = @auditor_perms_json,
            @granted_by_user_id = 2,
            @granted_by_role_name = N'SYSTEM',
            @audit_action = N'BOOTSTRAP_SEED_ROLEPERM_AUDITOR';

        DECLARE @guest_perms_json NVARCHAR(MAX) =
        (
            SELECT module, action
            FROM dbo.Permissions
            WHERE (module = N'DEVICE' AND action = N'READ')
               OR (module = N'POUR_PROFILE' AND action = N'READ')
               OR (module = N'POUR_SESSION' AND action = N'WRITE')
            ORDER BY module, action
            FOR JSON PATH
        );

        EXEC dbo.RolePerm_Grant_Batch
            @role_name = N'GUEST',
            @perm_list_json = @guest_perms_json,
            @granted_by_user_id = 2,
            @granted_by_role_name = N'SYSTEM',
            @audit_action = N'BOOTSTRAP_SEED_ROLEPERM_GUEST';

        /* =========================================================
           6) Assign roles (via proc, audited)
           ========================================================= */
        EXEC dbo.Role_Assign
            @target_user_id = 1,
            @role_name = N'GUEST',
            @assigned_by_user_id = 2,
            @assigned_by_role_name = N'SYSTEM',
            @audit_action = N'BOOTSTRAP_ASSIGN_ROLE_ANONYMOUS_GUEST';

        EXEC dbo.Role_Assign
            @target_user_id = @admin_id,
            @role_name = N'ADMIN',
            @assigned_by_user_id = 2,
            @assigned_by_role_name = N'SYSTEM',
            @audit_action = N'BOOTSTRAP_ASSIGN_ROLE_ADMIN_ADMIN';
        /* =========================================================
   Seed SensorType (insert missing)
   ========================================================= */

        

        INSERT INTO dbo.SensorType(sensor_name, unit) VALUES
        (N'ULTRASONIC', N'CM'),    -- HC-SR04 distance (cup presence / distance)
        (N'FLOW',       N'ML_S'),  -- flow rate (or pulses -> converted to ml/s in firmware/app)
        (N'LOADCELL',   N'G');     -- grams (1 ml water ≈ 1 g)
        -- Build diff_json (deterministic ordering)
        DECLARE @sensortypes_diff NVARCHAR(MAX) =
        (
        SELECT sensor_name, unit
        FROM dbo.SensorType
        ORDER BY sensor_name
        FOR JSON PATH
        );

        -- Audit append
        EXEC dbo.Audit_Append
        @actor_user_id   = 2,
        @actor_role_name = N'SYSTEM',
        @action          = N'BOOTSTRAP_SEED_SENSOR_TYPES',
        @object_type     = N'SensorType',
        @object_id       = 0,
        @diff_json       = @sensortypes_diff,
        @audit_id        = @audit_id OUTPUT,
        @chain_hash      = @chain_hash OUTPUT;
        
        EXEC dbo.Device_Create
        @location      = N'Main Lobby',
        @model         = N'ESP32-SmartPour',
        @firmware_ver  = N'1.0.0',
        @status        = N'ACTIVE',

        @actor_user_id   = 2,
        @actor_role_name = N'SYSTEM',
        @reason          = N'Bootstrap test device',

        @new_device_id   = @new_device_id OUTPUT,
        @audit_id        = @audit_id OUTPUT,
        @chain_hash      = @chain_hash OUTPUT;
        
        DECLARE
    @sensor_type_id INT;
    
SELECT @sensor_type_id = sensor_type_id
FROM dbo.SensorType
WHERE sensor_name = N'LOADCELL';
        EXEC dbo.Calibration_Create
    @device_id       = @new_device_id,
    @sensor_type_id  = @sensor_type_id,
    @factor          = 180,
    @offset          = 0,
    @created_by      = 2,                 -- SYSTEM
    @actor_role_name = N'SYSTEM',
    @reason          = N'Initial seed calibration',
    @new_calib_id    = @new_calib_id OUTPUT,
    @audit_id        = @audit_id OUTPUT,
    @chain_hash      = @chain_hash OUTPUT;

    SELECT @sensor_type_id = sensor_type_id
FROM dbo.SensorType
WHERE sensor_name = N'ULTRASONIC';
EXEC dbo.Calibration_Create
    @device_id       = @new_device_id,
    @sensor_type_id  = @sensor_type_id,
    @factor          = 1,
    @offset          = 0,
    @created_by      = 2,                 -- SYSTEM
    @actor_role_name = N'SYSTEM',
    @reason          = N'Initial seed calibration',
    @new_calib_id    = @new_calib_id OUTPUT,
    @audit_id        = @audit_id OUTPUT,
    @chain_hash      = @chain_hash OUTPUT;

    SELECT @sensor_type_id = st.sensor_type_id
FROM dbo.SensorType AS st
WHERE st.sensor_name = N'FLOW';

EXEC dbo.Calibration_Create
    @device_id       = @new_device_id,
    @sensor_type_id  = @sensor_type_id,
    @factor          = 450,
    @offset          = 0,
    @created_by      = 2,                 -- SYSTEM
    @actor_role_name = N'SYSTEM',
    @reason          = N'Initial seed calibration',
    @new_calib_id    = @new_calib_id OUTPUT,
    @audit_id        = @audit_id OUTPUT,
    @chain_hash      = @chain_hash OUTPUT;

    -- 90ml profile
EXEC dbo.PourProfile_Create
    @name = N'DEFAULT_90',
    @target_ml = 90,
    @tolerance_ml = 15,
    @max_duration_s = 10,
    @max_flow_rate = 65,
    @actor_user_id = 2,
    @actor_role_name = N'SYSTEM',
    @reason = N'Bootstrap default profile (90ml)',
    @new_profile_id = @new_profile_id OUTPUT,
    @audit_id = @audit_id OUTPUT,
    @chain_hash = @chain_hash OUTPUT;


-- 180ml profile
EXEC dbo.PourProfile_Create
    @name = N'DEFAULT_180',
    @target_ml = 180,
    @tolerance_ml = 15,
    @max_duration_s = 18,
    @max_flow_rate = 65,
    @actor_user_id = 2,
    @actor_role_name = N'SYSTEM',
    @reason = N'Bootstrap default profile (180ml)',
    @new_profile_id = @new_profile_id OUTPUT,
    @audit_id = @audit_id OUTPUT,
    @chain_hash = @chain_hash OUTPUT;


-- 270ml profile
EXEC dbo.PourProfile_Create
    @name = N'DEFAULT_270',
    @target_ml = 270,
    @tolerance_ml = 15,
    @max_duration_s = 24,
    @max_flow_rate = 65,
    @actor_user_id = 2,
    @actor_role_name = N'SYSTEM',
    @reason = N'Bootstrap default profile (270ml)',
    @new_profile_id = @new_profile_id OUTPUT,
    @audit_id = @audit_id OUTPUT,
    @chain_hash = @chain_hash OUTPUT;

EXEC dbo.PourProfile_Create
        @name = N'DEFAULT_360',
        @target_ml = 360,
        @tolerance_ml = 15,
        @max_duration_s = 30,
        @max_flow_rate = 65,
        @actor_user_id = 2,
        @actor_role_name = N'SYSTEM',
        @reason = N'Bootstrap default profile(360ml)',
        @new_profile_id = @new_profile_id OUTPUT,
        @audit_id = @audit_id OUTPUT,
        @chain_hash = @chain_hash OUTPUT;

        

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;
        THROW;
    END CATCH
END;
GO
--EXEC Bootstrap_Init

