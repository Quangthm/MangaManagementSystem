USE MangaManagementDB;
GO

/* ============================================================
   SCRUM-125 - Update user display name
   - Locks the profile update for one user.
   - Updates the display name.
   - Appends an audit event in the same transaction.
   ============================================================ */
CREATE OR ALTER PROCEDURE auth.usp_User_UpdateDisplayName
    @user_id UNIQUEIDENTIFIER,
    @display_name NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @lock_result INT;
    DECLARE @lock_resource NVARCHAR(255);

    DECLARE @old_display_name NVARCHAR(100);
    DECLARE @normalized_display_name NVARCHAR(100);

    DECLARE @audit_entity_id NVARCHAR(100);
    DECLARE @detail_json NVARCHAR(MAX);

    SET @normalized_display_name =
        NULLIF(LTRIM(RTRIM(@display_name)), N'');

    BEGIN TRY
        IF @normalized_display_name IS NULL
        BEGIN
            ;THROW 54101, 'Display name cannot be empty.', 1;
        END;

        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        SET @lock_resource =
            N'auth_user_display_name_update_'
            + CONVERT(NVARCHAR(36), @user_id);

        EXEC @lock_result = sys.sp_getapplock
            @Resource = @lock_resource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lock_result < 0
        BEGIN
            ;THROW 54102, 'Could not acquire display name update lock.', 1;
        END;

        SELECT
            @old_display_name = u.display_name
        FROM auth.Users u WITH (UPDLOCK, HOLDLOCK)
        WHERE u.user_id = @user_id;

        IF @@ROWCOUNT = 0
        BEGIN
            ;THROW 54103, 'User was not found.', 1;
        END;

        IF @old_display_name = @normalized_display_name
        BEGIN
            IF @started_tran = 1
            BEGIN
                COMMIT;
            END;

            RETURN;
        END;

        UPDATE auth.Users
        SET display_name = @normalized_display_name
        WHERE user_id = @user_id;

        SELECT @detail_json =
        (
            SELECT
                @user_id AS user_id,
                @old_display_name AS old_display_name,
                @normalized_display_name AS new_display_name
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        SET @audit_entity_id =
            CONVERT(NVARCHAR(36), @user_id);

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @user_id,
            @action_code = N'USER_DISPLAY_NAME_UPDATED',
            @entity_type = N'Users',
            @entity_id = @audit_entity_id,
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

        ;THROW;
    END CATCH;
END;
GO

/* ============================================================
   SCRUM-125 - Replace user avatar
   - Uses one user_id because Profile Settings is self-update.
   - Creates the new FileResource.
   - Locks the user's avatar update workflow.
   - Updates auth.Users.avatar_file_id.
   - Soft deletes the previous FileResource.
   - Appends audit events in the same SQL transaction.
   - Returns old Cloudinary metadata for C# cleanup.
   ============================================================ */
CREATE OR ALTER PROCEDURE auth.usp_User_UpdateAvatarFile
    @user_id UNIQUEIDENTIFIER,

    @original_file_name NVARCHAR(260),
    @cloudinary_public_id NVARCHAR(255),
    @cloudinary_secure_url NVARCHAR(1000),
    @content_type NVARCHAR(100),
    @file_size_bytes BIGINT,
    @sha256_hash CHAR(64),

    @new_avatar_file_id UNIQUEIDENTIFIER OUTPUT,

    @old_avatar_file_id UNIQUEIDENTIFIER = NULL OUTPUT,
    @old_cloudinary_public_id NVARCHAR(255) = NULL OUTPUT,
    @old_content_type NVARCHAR(100) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @lock_result INT;
    DECLARE @lock_resource NVARCHAR(255);

    DECLARE @old_file_purpose_code NVARCHAR(50);
    DECLARE @old_deleted_at_utc DATETIME2(0);

    DECLARE @audit_entity_id NVARCHAR(100);
    DECLARE @detail_json NVARCHAR(MAX);

    BEGIN TRY
        SET @new_avatar_file_id = NULL;
        SET @old_avatar_file_id = NULL;
        SET @old_cloudinary_public_id = NULL;
        SET @old_content_type = NULL;

        IF @user_id IS NULL
        BEGIN
            ;THROW 54201, 'user_id is required.', 1;
        END;

        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        SET @lock_resource =
            N'auth_user_avatar_update_'
            + CONVERT(NVARCHAR(36), @user_id);

        EXEC @lock_result = sys.sp_getapplock
            @Resource = @lock_resource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lock_result < 0
        BEGIN
            ;THROW 54202, 'Could not acquire avatar update lock.', 1;
        END;

        SELECT
            @old_avatar_file_id = u.avatar_file_id
        FROM auth.Users u WITH (UPDLOCK, HOLDLOCK)
        WHERE u.user_id = @user_id;

        IF @@ROWCOUNT = 0
        BEGIN
            ;THROW 54203, 'User was not found.', 1;
        END;

        EXEC manga.usp_FileResource_Create
            @file_purpose_code = N'USER_AVATAR',
            @original_file_name = @original_file_name,
            @cloudinary_public_id = @cloudinary_public_id,
            @cloudinary_secure_url = @cloudinary_secure_url,
            @content_type = @content_type,
            @file_size_bytes = @file_size_bytes,
            @sha256_hash = @sha256_hash,
            @uploaded_by_user_id = @user_id,
            @file_resource_id = @new_avatar_file_id OUTPUT;

        IF @new_avatar_file_id IS NULL
        BEGIN
            ;THROW 54204, 'The new avatar FileResource could not be created.', 1;
        END;

        UPDATE auth.Users
        SET avatar_file_id = @new_avatar_file_id
        WHERE user_id = @user_id;

        IF @old_avatar_file_id IS NOT NULL
        BEGIN
            SELECT
                @old_file_purpose_code = fr.file_purpose_code,
                @old_cloudinary_public_id = fr.cloudinary_public_id,
                @old_content_type = fr.content_type,
                @old_deleted_at_utc = fr.deleted_at_utc
            FROM manga.FileResource fr WITH (UPDLOCK, HOLDLOCK)
            WHERE fr.file_resource_id = @old_avatar_file_id;

            IF @old_file_purpose_code IS NULL
            BEGIN
                ;THROW 54205, 'The previous avatar FileResource was not found.', 1;
            END;

            IF @old_file_purpose_code <> N'USER_AVATAR'
            BEGIN
                ;THROW 54206, 'The previous file is not a USER_AVATAR resource.', 1;
            END;

            IF @old_deleted_at_utc IS NULL
            BEGIN
                EXEC manga.usp_FileResource_SoftDelete
                    @file_resource_id = @old_avatar_file_id,
                    @deleted_by_user_id = @user_id,
                    @delete_reason = N'Replaced by a new user avatar.';
            END;
        END;

        SELECT @detail_json =
        (
            SELECT
                @user_id AS user_id,
                @old_avatar_file_id AS old_avatar_file_id,
                @new_avatar_file_id AS new_avatar_file_id,
                @old_cloudinary_public_id AS old_cloudinary_public_id,
                @cloudinary_public_id AS new_cloudinary_public_id,
                @original_file_name AS new_original_file_name,
                @content_type AS new_content_type,
                @file_size_bytes AS new_file_size_bytes
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        SET @audit_entity_id =
            CONVERT(NVARCHAR(36), @user_id);

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @user_id,
            @action_code = N'USER_AVATAR_UPDATED',
            @entity_type = N'Users',
            @entity_id = @audit_entity_id,
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

        ;THROW;
    END CATCH;
END;
GO

/* ============================================================
   SCRUM-125 - Replace user portfolio
   - Uses one user_id because Profile Settings is self-update.
   - Creates the new FileResource.
   - Locks the user's portfolio update workflow.
   - Updates auth.Users.portfolio_file_id.
   - Soft deletes the previous FileResource.
   - Appends audit events in the same SQL transaction.
   - Returns old Cloudinary metadata for C# cleanup.
   ============================================================ */
CREATE OR ALTER PROCEDURE auth.usp_User_UpdatePortfolioFile
    @user_id UNIQUEIDENTIFIER,

    @original_file_name NVARCHAR(260),
    @cloudinary_public_id NVARCHAR(255),
    @cloudinary_secure_url NVARCHAR(1000),
    @content_type NVARCHAR(100),
    @file_size_bytes BIGINT,
    @sha256_hash CHAR(64),

    @new_portfolio_file_id UNIQUEIDENTIFIER OUTPUT,

    @old_portfolio_file_id UNIQUEIDENTIFIER = NULL OUTPUT,
    @old_cloudinary_public_id NVARCHAR(255) = NULL OUTPUT,
    @old_content_type NVARCHAR(100) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @lock_result INT;
    DECLARE @lock_resource NVARCHAR(255);

    DECLARE @old_file_purpose_code NVARCHAR(50);
    DECLARE @old_deleted_at_utc DATETIME2(0);

    DECLARE @audit_entity_id NVARCHAR(100);
    DECLARE @detail_json NVARCHAR(MAX);

    BEGIN TRY
        SET @new_portfolio_file_id = NULL;
        SET @old_portfolio_file_id = NULL;
        SET @old_cloudinary_public_id = NULL;
        SET @old_content_type = NULL;

        IF @user_id IS NULL
        BEGIN
            ;THROW 54301, 'user_id is required.', 1;
        END;

        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        SET @lock_resource =
            N'auth_user_portfolio_update_'
            + CONVERT(NVARCHAR(36), @user_id);

        EXEC @lock_result = sys.sp_getapplock
            @Resource = @lock_resource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lock_result < 0
        BEGIN
            ;THROW 54302, 'Could not acquire portfolio update lock.', 1;
        END;

        SELECT
            @old_portfolio_file_id = u.portfolio_file_id
        FROM auth.Users u WITH (UPDLOCK, HOLDLOCK)
        WHERE u.user_id = @user_id;

        IF @@ROWCOUNT = 0
        BEGIN
            ;THROW 54303, 'User was not found.', 1;
        END;

        EXEC manga.usp_FileResource_Create
            @file_purpose_code = N'REGISTRATION_PORTFOLIO',
            @original_file_name = @original_file_name,
            @cloudinary_public_id = @cloudinary_public_id,
            @cloudinary_secure_url = @cloudinary_secure_url,
            @content_type = @content_type,
            @file_size_bytes = @file_size_bytes,
            @sha256_hash = @sha256_hash,
            @uploaded_by_user_id = @user_id,
            @file_resource_id = @new_portfolio_file_id OUTPUT;

        IF @new_portfolio_file_id IS NULL
        BEGIN
            ;THROW 54304, 'The new portfolio FileResource could not be created.', 1;
        END;

        UPDATE auth.Users
        SET portfolio_file_id = @new_portfolio_file_id
        WHERE user_id = @user_id;

        IF @old_portfolio_file_id IS NOT NULL
        BEGIN
            SELECT
                @old_file_purpose_code = fr.file_purpose_code,
                @old_cloudinary_public_id = fr.cloudinary_public_id,
                @old_content_type = fr.content_type,
                @old_deleted_at_utc = fr.deleted_at_utc
            FROM manga.FileResource fr WITH (UPDLOCK, HOLDLOCK)
            WHERE fr.file_resource_id = @old_portfolio_file_id;

            IF @old_file_purpose_code IS NULL
            BEGIN
                ;THROW 54305, 'The previous portfolio FileResource was not found.', 1;
            END;

            IF @old_file_purpose_code <> N'REGISTRATION_PORTFOLIO'
            BEGIN
                ;THROW 54306, 'The previous file is not a REGISTRATION_PORTFOLIO resource.', 1;
            END;

            IF @old_deleted_at_utc IS NULL
            BEGIN
                EXEC manga.usp_FileResource_SoftDelete
                    @file_resource_id = @old_portfolio_file_id,
                    @deleted_by_user_id = @user_id,
                    @delete_reason = N'Replaced by a new user portfolio.';
            END;
        END;

        SELECT @detail_json =
        (
            SELECT
                @user_id AS user_id,
                @old_portfolio_file_id AS old_portfolio_file_id,
                @new_portfolio_file_id AS new_portfolio_file_id,
                @old_cloudinary_public_id AS old_cloudinary_public_id,
                @cloudinary_public_id AS new_cloudinary_public_id,
                @original_file_name AS new_original_file_name,
                @content_type AS new_content_type,
                @file_size_bytes AS new_file_size_bytes
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        SET @audit_entity_id =
            CONVERT(NVARCHAR(36), @user_id);

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @user_id,
            @action_code = N'USER_PORTFOLIO_UPDATED',
            @entity_type = N'Users',
            @entity_id = @audit_entity_id,
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

        ;THROW;
    END CATCH;
END;
GO

/* ============================================================
   SCRUM-125 - Reset user password
   - Locks password reset for one user.
   - Updates only password_hash.
   - Does not expose the password hash in audit JSON.
   ============================================================ */
CREATE OR ALTER PROCEDURE auth.usp_User_ResetPassword
    @user_id UNIQUEIDENTIFIER,
    @password_hash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;
    DECLARE @lock_result INT;
    DECLARE @lock_resource NVARCHAR(255);

    DECLARE @audit_entity_id NVARCHAR(100);
    DECLARE @detail_json NVARCHAR(MAX);

    BEGIN TRY
        IF @password_hash IS NULL
           OR LTRIM(RTRIM(@password_hash)) = N''
        BEGIN
            ;THROW 52101, 'Password hash cannot be empty.', 1;
        END;

        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        SET @lock_resource =
            N'auth_user_password_reset_'
            + CONVERT(NVARCHAR(36), @user_id);

        EXEC @lock_result = sys.sp_getapplock
            @Resource = @lock_resource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lock_result < 0
        BEGIN
            ;THROW 52102, 'Could not acquire password reset lock.', 1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM auth.Users WITH (UPDLOCK, HOLDLOCK)
            WHERE user_id = @user_id
        )
        BEGIN
            ;THROW 52103, 'User was not found.', 1;
        END;

        UPDATE auth.Users
        SET password_hash = @password_hash
        WHERE user_id = @user_id;

        SELECT @detail_json =
        (
            SELECT
                @user_id AS user_id,
                SYSUTCDATETIME() AS password_reset_at_utc
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        SET @audit_entity_id =
            CONVERT(NVARCHAR(36), @user_id);

        EXEC audit.usp_AuditEvent_Append
            @actor_user_id = @user_id,
            @action_code = N'USER_PASSWORD_RESET',
            @entity_type = N'Users',
            @entity_id = @audit_entity_id,
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

        ;THROW;
    END CATCH;
END;
GO