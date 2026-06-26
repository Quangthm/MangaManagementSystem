-- ============================================================
-- Pending Database Updates for MangaManagementSystem
-- Apply these against your target database before smoke testing
-- ============================================================
-- Generated: 2026-06-25
-- Branch: Fix/UI
-- ============================================================

-- ============================================================
-- 1. PageRegion constraint updates (FULL_PAGE support)
--    Source: docs/revision/Mangaka/2026-06-25-page-region-full-page-and-cloudinary-bounds.md
-- ============================================================
PRINT 'Applying PageRegion constraint updates...';

-- Drop old constraint
ALTER TABLE manga.PageRegion DROP CONSTRAINT IF EXISTS CK_PageRegion_Width_Height;

-- Drop old type check, add new one with FULL_PAGE
ALTER TABLE manga.PageRegion DROP CONSTRAINT IF EXISTS CK_PageRegion_TypeCode;
ALTER TABLE manga.PageRegion ADD CONSTRAINT CK_PageRegion_TypeCode
    CHECK (type_code IN (N'PANEL', N'SPEECH_BUBBLE', N'CHARACTER', N'SFX_TEXT', N'BACKGROUND', N'FULL_PAGE', N'OTHER'));

-- Add new constraints
ALTER TABLE manga.PageRegion ADD CONSTRAINT CK_PageRegion_Coordinates_NonNegative
    CHECK (x >= 0 AND y >= 0);

ALTER TABLE manga.PageRegion ADD CONSTRAINT CK_PageRegion_Dimensions
    CHECK ((width = 0 AND height = 0) OR (width > 0 AND height > 0));

ALTER TABLE manga.PageRegion ADD CONSTRAINT CK_PageRegion_FullPageShape
    CHECK (type_code <> N'FULL_PAGE' OR (x = 0 AND y = 0 AND width > 0 AND height > 0));

-- Set default values
ALTER TABLE manga.PageRegion ADD DEFAULT 0 FOR width;
ALTER TABLE manga.PageRegion ADD DEFAULT 0 FOR height;

PRINT 'PageRegion constraints applied OK.';

-- ============================================================
-- 2. usp_SeriesContributor_EndAssistant
--    Source: MangaManagementSystem_Procedures_Views_Bootstrap.sql
--    Note: This SP already exists in the bootstrap script but
--          may not have been deployed yet.
-- ============================================================
PRINT 'Ensuring usp_SeriesContributor_EndAssistant exists...';

CREATE OR ALTER PROCEDURE manga.usp_SeriesContributor_EndAssistant
    @actor_user_id UNIQUEIDENTIFIER,
    @series_id UNIQUEIDENTIFIER,
    @assistant_user_id UNIQUEIDENTIFIER,
    @reason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @transaction_id INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Acquire app lock for contributor management on this series
        EXEC @transaction_id = sp_getapplock
            @Resource = 'manga_SeriesContributor_End',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 5000;

        IF @transaction_id < 0
            THROW 58601, N'Could not acquire lock. Please try again.', 1;

        -- Verify actor is an active Mangaka contributor
        IF NOT EXISTS (
            SELECT 1 FROM manga.SeriesContributor sc
            INNER JOIN auth.Users u ON u.user_id = sc.user_id
            INNER JOIN auth.Roles r ON r.role_id = u.role_id
            WHERE sc.series_id = @series_id
              AND sc.user_id = @actor_user_id
              AND sc.end_date IS NULL
              AND u.status_code = N'ACTIVE'
              AND r.role_name = N'Mangaka'
        )
            THROW 58602, N'You are not an active Mangaka contributor for this series.', 1;

        -- Verify target user exists, is ACTIVE, and has role Assistant
        IF NOT EXISTS (
            SELECT 1 FROM auth.Users u
            INNER JOIN auth.Roles r ON r.role_id = u.role_id
            WHERE u.user_id = @assistant_user_id
              AND u.status_code = N'ACTIVE'
              AND r.role_name = N'Assistant'
        )
            THROW 58603, N'Target user is not an active Assistant.', 1;

        -- Verify target is currently an active contributor
        IF NOT EXISTS (
            SELECT 1 FROM manga.SeriesContributor
            WHERE series_id = @series_id
              AND user_id = @assistant_user_id
              AND end_date IS NULL
        )
            THROW 58604, N'Target user is not an active contributor of this series.', 1;

        -- Block if assistant has ASSIGNED or UNDER_REVIEW tasks for this series
        IF EXISTS (
            SELECT 1 FROM manga.ChapterPageTask cpt
            INNER JOIN manga.ChapterPageTaskRegion cptr ON cptr.chapter_page_task_id = cpt.chapter_page_task_id
            INNER JOIN manga.PageRegion pr ON pr.page_region_id = cptr.page_region_id
            INNER JOIN manga.ChapterPageVersion cpv ON cpv.chapter_page_version_id = pr.chapter_page_version_id
            INNER JOIN manga.ChapterPage cp ON cp.chapter_page_id = cpv.chapter_page_id
            INNER JOIN manga.Chapter c ON c.chapter_id = cp.chapter_id
            WHERE c.series_id = @series_id
              AND cpt.assigned_to_user_id = @assistant_user_id
              AND cpt.status_code IN (N'ASSIGNED', N'UNDER_REVIEW')
        )
            THROW 58605, N'This assistant has active tasks. Reassign or cancel their tasks before removing them from the series.', 1;

        -- End the contributor row
        UPDATE manga.SeriesContributor
        SET end_date = SYSDATETIME()
        WHERE series_id = @series_id
          AND user_id = @assistant_user_id
          AND end_date IS NULL;

        -- Audit log
        INSERT INTO audit.AuditEvent (event_id, actor_user_id, action_code, entity_type, entity_id, [description], occurred_at_utc)
        VALUES (NEWID(), @actor_user_id, N'SERIES_CONTRIBUTOR_ASSISTANT_ENDED', N'SeriesContributor',
                (SELECT TOP 1 series_contributor_id FROM manga.SeriesContributor
                 WHERE series_id = @series_id AND user_id = @assistant_user_id AND end_date IS NOT NULL
                 ORDER BY end_date DESC),
                N'Assistant contributor ended: ' + @reason, SYSUTCDATETIME());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;

PRINT 'usp_SeriesContributor_EndAssistant applied OK.';

PRINT '';
PRINT '============================================================';
PRINT 'All pending DB updates applied successfully.';
PRINT '============================================================';
PRINT '';
GO
