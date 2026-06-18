USE MangaManagementDB;
GO
CREATE OR ALTER PROCEDURE audit.usp_AuditEvent_Append
    @actor_user_id      UNIQUEIDENTIFIER = NULL,
    @action_code        NVARCHAR(64),
    @entity_type        NVARCHAR(128),
    @entity_id          NVARCHAR(100) = NULL,
    @detail_json        NVARCHAR(MAX) = NULL,
    @audit_event_id     BIGINT = NULL OUTPUT
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
            ;THROW 50001, 'detail_json must be valid JSON.', 1;
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
            FROM
            (
                SELECT
                    u.user_id,
                    u.role_id
                FROM auth.Users u
                WHERE u.user_id = @actor_user_id
            ) AS actor_user
            INNER JOIN auth.Roles r
                ON r.role_id = actor_user.role_id;

            IF @actor_role_name IS NULL
            BEGIN
                ;THROW 50003, 'Actor user role could not be resolved.', 1;
            END;
        END;

        EXEC @lock_result = sys.sp_getapplock
            @Resource = N'audit_event_append',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lock_result < 0
        BEGIN
            ;THROW 50002, 'Could not acquire audit event append lock.', 1;
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

        SET @audit_event_id =
            CONVERT(
                BIGINT,
                SCOPE_IDENTITY());

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
    END CATCH
END;
GO
