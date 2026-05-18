USE MangaManagementDB;
GO
CREATE OR ALTER PROCEDURE dbo.Audit_Append
    @actor_user_id INT,
    @actor_role_name NVARCHAR(30),
    @action NVARCHAR(50),
    @object_type NVARCHAR(50),
    @object_id INT,
    @diff_json NVARCHAR(MAX) = NULL,
    @audit_id INT OUTPUT,
    @chain_hash CHAR(64) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;

    DECLARE @prev_hash CHAR(64) = NULL;
    DECLARE @row_text NVARCHAR(MAX);
    DECLARE @row_hash CHAR(64);
    DECLARE @timestamp_utc DATETIME2(7);

    DECLARE @zero_hash CHAR(64) = REPLICATE('0', 64);

    DECLARE @ins TABLE (audit_id INT, timestamp_utc DATETIME2(7));

    BEGIN TRY
        --------------------------------------------------------------------
        -- Transaction ownership:
        -- If caller already started a transaction, we participate in it.
        -- Otherwise, we start and commit our own.
        --------------------------------------------------------------------
        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END

        --------------------------------------------------------------------
        -- 1) Insert AuditLog and capture the DB-generated UTC timestamp
        --------------------------------------------------------------------
        INSERT INTO dbo.AuditLog (
            actor_user_id, actor_role_name, action, object_type, object_id, diff_json
        )
        OUTPUT inserted.audit_id, inserted.[timestamp]
        INTO @ins(audit_id, timestamp_utc)
        VALUES (
            @actor_user_id, @actor_role_name, @action, @object_type, @object_id, @diff_json
        );

        SELECT TOP (1)
            @audit_id = audit_id,
            @timestamp_utc = timestamp_utc
        FROM @ins;

        --------------------------------------------------------------------
        -- 2) Read previous chain tip with locks to prevent forks
        --------------------------------------------------------------------
        SELECT TOP (1) @prev_hash = chain_hash
        FROM dbo.HashChain WITH (UPDLOCK, HOLDLOCK)
        ORDER BY anchor_id DESC;

        --------------------------------------------------------------------
        -- 3) Canonical row representation (stable ordering, explicit NULLs)
        --------------------------------------------------------------------
        SET @row_text = CONCAT(
            'audit_id=', @audit_id, '|',
            'timestamp_utc=', CONVERT(NVARCHAR(33), @timestamp_utc, 126), '|',
            'actor_user_id=', @actor_user_id, '|',
            'actor_role=', @actor_role_name, '|',
            'action=', @action, '|',
            'object_type=', @object_type, '|',
            'object_id=', @object_id, '|',
            'diff=', COALESCE(@diff_json, '<NULL>')
        );

        --------------------------------------------------------------------
        -- 4) row_hash = SHA256( UTF-16LE bytes of NVARCHAR row_text )
        --------------------------------------------------------------------
        SET @row_hash = CONVERT(CHAR(64),
            HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), @row_text)),
        2);

        --------------------------------------------------------------------
        -- 5) chain_hash = SHA256( prev_or_zero | row_hash )
        --------------------------------------------------------------------
        DECLARE @chain_input NVARCHAR(MAX) =
            CONCAT(COALESCE(@prev_hash, @zero_hash), '|', @row_hash);

        SET @chain_hash = CONVERT(CHAR(64),
            HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), @chain_input)),
        2);

        --------------------------------------------------------------------
        -- 6) Persist chain link (1-to-1 with AuditLog via UNIQUE(audit_id))
        --------------------------------------------------------------------
        INSERT INTO dbo.HashChain (audit_id, prev_hash, row_hash, chain_hash)
        VALUES (@audit_id, @prev_hash, @row_hash, @chain_hash);

        --------------------------------------------------------------------
        -- Commit only if we started the transaction
        --------------------------------------------------------------------
        IF @started_tran = 1
            COMMIT;
    END TRY
    BEGIN CATCH
        --------------------------------------------------------------------
        -- Rollback only if we started it.
        -- If caller owns the transaction, let caller decide rollback/commit.
        --------------------------------------------------------------------
        IF @started_tran = 1 AND XACT_STATE() <> 0
            ROLLBACK;

        THROW;
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.Role_Assign
    @target_user_id          INT,
    @role_name               NVARCHAR(30),
    @assigned_by_user_id     INT,
    @assigned_by_role_name   NVARCHAR(30),
    @audit_action NVARCHAR(80) = N'ASSIGN_ROLE'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @started_tran BIT = 0;

    DECLARE @role_id     INT;
    DECLARE @audit_id    INT;
    DECLARE @chain_hash  CHAR(64);
    DECLARE @diff        NVARCHAR(MAX);
    DECLARE @rn NVARCHAR(30) = UPPER(LTRIM(RTRIM(@role_name)));

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END
        SELECT @role_id = r.role_id
        FROM dbo.Roles r
        WHERE r.role_name = @rn;

        IF @role_id IS NULL
            THROW 50001, 'Role not found.', 1;

        -- Ensure not already active
        IF EXISTS (
            SELECT 1
            FROM dbo.UserRole ur
            WHERE ur.user_id = @target_user_id
              AND ur.role_id = @role_id
              AND ur.revoked_at IS NULL
        )
            THROW 50005, 'Role already assigned (active).', 1;

        INSERT INTO dbo.UserRole (user_id, role_id, assigned_by_user_id)
        VALUES (@target_user_id, @role_id, @assigned_by_user_id);

        SELECT @diff =
        (
            SELECT
                @target_user_id AS target_user_id,
                @rn      AS role
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC dbo.Audit_Append
            @actor_user_id   = @assigned_by_user_id,
            @actor_role_name = @assigned_by_role_name,
            @action          = @audit_action,
            @object_type     = N'UserRole',
            @object_id       = @target_user_id,
            @diff_json       = @diff,
            @audit_id        = @audit_id OUTPUT,
            @chain_hash      = @chain_hash OUTPUT;

        IF @started_tran = 1
        COMMIT;    
    END TRY
    BEGIN CATCH
        IF @started_tran = 1 AND XACT_STATE() <> 0
            ROLLBACK;
        THROW;
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.Role_Revoke
    @target_user_id          INT,
    @role_name               NVARCHAR(30),
    @revoked_by_user_id      INT,
    @revoked_by_role_name    NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @role_id INT;
    DECLARE @audit_id INT;
    DECLARE @chain_hash CHAR(64);
    DECLARE @diff NVARCHAR(MAX);
    DECLARE @normalized_role NVARCHAR(30);

    BEGIN TRY
        BEGIN TRAN;

        /* 0) Normalize inputs */
        SET @normalized_role = UPPER(LTRIM(RTRIM(@role_name)));

        /* 1) Policy guard: prevent ADMIN role revocation (simple governance rule) */
        IF @normalized_role = N'ADMIN'
            THROW 54001, 'Revoking ADMIN role is not permitted by policy.', 1;

        /* 2) Resolve role */
        SELECT @role_id = r.role_id
        FROM dbo.Roles AS r
        WHERE r.role_name = @normalized_role;

        IF @role_id IS NULL
            THROW 50002, 'Role not found.', 1;

        /* 3) Identify the latest active assignment row */
        DECLARE @assigned_at DATETIME2(7);

        SELECT TOP (1) @assigned_at = ur.assigned_at
        FROM dbo.UserRole AS ur
        WHERE ur.user_id = @target_user_id
          AND ur.role_id = @role_id
          AND ur.revoked_at IS NULL
        ORDER BY ur.assigned_at DESC;

        IF @assigned_at IS NULL
            THROW 50003, 'Active role assignment not found.', 1;

        /* 4) Revoke that exact row (race-safe) */
        UPDATE dbo.UserRole
        SET revoked_at = SYSUTCDATETIME()
        WHERE user_id = @target_user_id
          AND role_id = @role_id
          AND assigned_at = @assigned_at
          AND revoked_at IS NULL;

        IF @@ROWCOUNT = 0
            THROW 50003, 'Active role assignment not found.', 1;

        /* 5) Build diff_json safely */
        SELECT @diff =
        (
            SELECT
                @target_user_id     AS target_user_id,
                @normalized_role    AS role
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        /* 6) Append audit */
        EXEC dbo.Audit_Append
            @actor_user_id   = @revoked_by_user_id,
            @actor_role_name = @revoked_by_role_name,
            @action          = N'REVOKE_ROLE',
            @object_type     = N'UserRole',
            @object_id       = @target_user_id,
            @diff_json       = @diff,
            @audit_id        = @audit_id OUTPUT,
            @chain_hash      = @chain_hash OUTPUT;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
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
CREATE OR ALTER PROCEDURE dbo.User_Create
    @username             NVARCHAR(50),
    @password_hash        NVARCHAR(255),
    @status               NVARCHAR(20) = NULL,     -- NULL => let DB default apply
    @created_by_user_id   INT,
    @created_by_role_name NVARCHAR(30),
    @new_user_id          INT OUTPUT,
    @audit_action NVARCHAR(80) = N'CREATE_USER'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @started_tran BIT = 0;
    DECLARE @audit_id   INT;
    DECLARE @chain_hash CHAR(64);
    DECLARE @diff_json  NVARCHAR(MAX);

    DECLARE @u NVARCHAR(50) = LTRIM(RTRIM(@username));

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END
        -- Insert + capture identity and effective status
        DECLARE @ins TABLE (user_id INT, status NVARCHAR(20));

        IF @status IS NULL
        BEGIN
            INSERT INTO dbo.Users (username, password_hash)
            OUTPUT inserted.user_id, inserted.status INTO @ins(user_id, status)
            VALUES (@u, @password_hash);
        END
        ELSE
        BEGIN
            INSERT INTO dbo.Users (username, password_hash, status)
            OUTPUT inserted.user_id, inserted.status INTO @ins(user_id, status)
            VALUES (@u, @password_hash, @status);
        END

        DECLARE @effective_status NVARCHAR(20);

        SELECT TOP (1)
            @new_user_id = user_id,
            @effective_status = status
        FROM @ins;

        -- Build audit diff (do NOT include password_hash)
        SELECT @diff_json =
        (
            SELECT
                @new_user_id        AS new_user_id,
                @u                  AS username,
                @effective_status   AS status
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        -- Audit append in the same transaction (atomic with user creation)
        EXEC dbo.Audit_Append
            @actor_user_id   = @created_by_user_id,
            @actor_role_name = @created_by_role_name,
            @action          = @audit_action,
            @object_type     = N'Users',
            @object_id       = @new_user_id,
            @diff_json       = @diff_json,
            @audit_id        = @audit_id OUTPUT,
            @chain_hash      = @chain_hash OUTPUT;

        IF @started_tran = 1
        COMMIT;
    END TRY
    BEGIN CATCH
        DECLARE @err INT = ERROR_NUMBER();

        IF @started_tran = 1 AND XACT_STATE() <> 0
            ROLLBACK;

        -- Unique username violation
        IF @err IN (2601, 2627)
            THROW 50011, 'Username already exists.', 1;

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

