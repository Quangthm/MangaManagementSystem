USE [MangaManagementDB];
GO

/*
    SCRUM-116 Assistant workflow local test data seed.

    IMPORTANT:
    - Select the correct MangaManagementDB database in SSMS.
    - Do NOT run this script in master.
    - Default mode is DRY RUN: @CommitChanges = 0, so changes are rolled back.
    - Change @CommitChanges to 1 only after the dry run SELECT results look correct.

    This script creates DATA only:
    - It does not create roles.
    - It does not create users.
    - It does not create password hashes.
    - It does not alter schema.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() = N'master'
    THROW 51000, 'Do not run this script in master database.', 1;

DECLARE @CommitChanges bit = 0; -- 0 = dry run rollback, 1 = commit

DECLARE @Now datetime2(0) = SYSUTCDATETIME();

DECLARE @AssistantUserId uniqueidentifier;
DECLARE @MangakaUserId uniqueidentifier;
DECLARE @SeriesId uniqueidentifier;
DECLARE @ChapterId uniqueidentifier;

DECLARE @SeriesTitle nvarchar(200) = N'SCRUM-116 Test Series';
DECLARE @SeriesSlug nvarchar(220) = N'scrum-116-test-series';
DECLARE @ChapterNumberLabel nvarchar(20) = N'1';
DECLARE @ChapterTitle nvarchar(200) = N'SCRUM-116 Test Chapter';

BEGIN TRY
    BEGIN TRANSACTION;

    -------------------------------------------------------------------------
    -- Validate existing Assistant user: troly
    -------------------------------------------------------------------------
    SELECT @AssistantUserId = u.user_id
    FROM auth.Users u
    INNER JOIN auth.Roles r ON r.role_id = u.role_id
    WHERE u.username = N'troly'
      AND r.role_name = N'Assistant'
      AND u.status_code = N'ACTIVE';

    IF @AssistantUserId IS NULL
        THROW 51001, 'Required ACTIVE Assistant user "troly" was not found. Create/login/approve troly first; this seed script does not create users.', 1;

    -------------------------------------------------------------------------
    -- Locate existing Mangaka user, prefer khoavq
    -------------------------------------------------------------------------
    SELECT TOP (1) @MangakaUserId = u.user_id
    FROM auth.Users u
    INNER JOIN auth.Roles r ON r.role_id = u.role_id
    WHERE r.role_name = N'Mangaka'
      AND u.status_code = N'ACTIVE'
      AND u.username = N'khoavq';

    IF @MangakaUserId IS NULL
    BEGIN
        SELECT TOP (1) @MangakaUserId = u.user_id
        FROM auth.Users u
        INNER JOIN auth.Roles r ON r.role_id = u.role_id
        WHERE r.role_name = N'Mangaka'
          AND u.status_code = N'ACTIVE'
        ORDER BY u.created_at_utc ASC;
    END;

    IF @MangakaUserId IS NULL
        THROW 51002, 'No ACTIVE Mangaka user was found. Create/approve a Mangaka user first; this seed script does not create users.', 1;

    -------------------------------------------------------------------------
    -- Create or reuse test series.
    -- Note: manga.Series does not allow APPROVED. SERIALIZED is the closest
    -- approved/live status for a series in the current schema.
    -------------------------------------------------------------------------
    SELECT @SeriesId = series_id
    FROM manga.Series
    WHERE slug = @SeriesSlug
       OR title = @SeriesTitle;

    IF @SeriesId IS NULL
    BEGIN
        SET @SeriesId = NEWID();

        INSERT INTO manga.Series
        (
            series_id,
            title,
            slug,
            synopsis,
            genre,
            status_code,
            content_language_code,
            created_at_utc,
            publication_frequency_code
        )
        VALUES
        (
            @SeriesId,
            @SeriesTitle,
            @SeriesSlug,
            N'Local SCRUM-116 test series for Assistant task workflow verification.',
            N'Test',
            N'SERIALIZED',
            N'vi',
            @Now,
            N'WEEKLY'
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM manga.SeriesContributor
        WHERE series_id = @SeriesId
          AND user_id = @MangakaUserId
          AND end_date IS NULL
    )
    BEGIN
        INSERT INTO manga.SeriesContributor
        (
            series_id,
            user_id,
            start_date,
            notes
        )
        VALUES
        (
            @SeriesId,
            @MangakaUserId,
            CONVERT(date, @Now),
            N'SCRUM-116 local test data owner/contributor.'
        );
    END;

    -------------------------------------------------------------------------
    -- Create or reuse test chapter.
    -------------------------------------------------------------------------
    SELECT @ChapterId = chapter_id
    FROM manga.Chapter
    WHERE series_id = @SeriesId
      AND chapter_number_label = @ChapterNumberLabel;

    IF @ChapterId IS NULL
    BEGIN
        SET @ChapterId = NEWID();

        INSERT INTO manga.Chapter
        (
            chapter_id,
            series_id,
            chapter_number_label,
            chapter_title,
            status_code,
            created_at_utc,
            created_by_user_id
        )
        VALUES
        (
            @ChapterId,
            @SeriesId,
            @ChapterNumberLabel,
            @ChapterTitle,
            N'APPROVED',
            @Now,
            @MangakaUserId
        );
    END;

    -------------------------------------------------------------------------
    -- Create/reuse 5 pages, file resources, page versions, regions, tasks,
    -- task-region links, and task assignment notifications.
    -------------------------------------------------------------------------
    DECLARE @PageNo int = 1;

    WHILE @PageNo <= 5
    BEGIN
        DECLARE @ChapterPageId uniqueidentifier = NULL;
        DECLARE @FileResourceId uniqueidentifier = NULL;
        DECLARE @PageVersionId uniqueidentifier = NULL;
        DECLARE @PageRegionId uniqueidentifier = NULL;
        DECLARE @TaskId uniqueidentifier = NULL;

        DECLARE @FilePublicId nvarchar(255) = CONCAT(N'local/scrum-116/', CONVERT(nvarchar(36), @ChapterId), N'/page-', @PageNo);
        DECLARE @FileName nvarchar(260) = CONCAT(N'scrum-116-page-', @PageNo, N'.png');
        DECLARE @TaskTitle nvarchar(200) = CONCAT(N'Coloring Task for Page ', @PageNo);

        ---------------------------------------------------------------------
        -- Chapter page
        ---------------------------------------------------------------------
        SELECT @ChapterPageId = chapter_page_id
        FROM manga.ChapterPage
        WHERE chapter_id = @ChapterId
          AND page_no = @PageNo
          AND deleted_at_utc IS NULL;

        IF @ChapterPageId IS NULL
        BEGIN
            SET @ChapterPageId = NEWID();

            INSERT INTO manga.ChapterPage
            (
                chapter_page_id,
                chapter_id,
                page_no,
                page_notes
            )
            VALUES
            (
                @ChapterPageId,
                @ChapterId,
                @PageNo,
                N'SCRUM-116 local test page.'
            );
        END;

        ---------------------------------------------------------------------
        -- FileResource + current ChapterPageVersion.
        -- A dummy Cloudinary file row is used only so ChapterPageVersion can
        -- satisfy its NOT NULL page_file_id FK for local workflow testing.
        ---------------------------------------------------------------------
        SELECT TOP (1)
            @PageVersionId = cpv.chapter_page_version_id,
            @FileResourceId = cpv.page_file_id
        FROM manga.ChapterPageVersion cpv
        WHERE cpv.chapter_page_id = @ChapterPageId
          AND cpv.is_current_version = 1
        ORDER BY cpv.version_no DESC;

        IF @PageVersionId IS NULL
        BEGIN
            SELECT @FileResourceId = file_resource_id
            FROM manga.FileResource
            WHERE cloudinary_public_id = @FilePublicId
              AND deleted_at_utc IS NULL;

            IF @FileResourceId IS NULL
            BEGIN
                SET @FileResourceId = NEWID();

                INSERT INTO manga.FileResource
                (
                    file_resource_id,
                    file_purpose_code,
                    original_file_name,
                    cloudinary_public_id,
                    cloudinary_secure_url,
                    content_type,
                    file_size_bytes,
                    sha256_hash,
                    uploaded_by_user_id,
                    uploaded_at_utc
                )
                VALUES
                (
                    @FileResourceId,
                    N'CHAPTER_PAGE_VERSION',
                    @FileName,
                    @FilePublicId,
                    CONCAT(N'https://example.local/mangaflow/scrum-116/page-', @PageNo, N'.png'),
                    N'image/png',
                    1,
                    REPLICATE('0', 64),
                    @MangakaUserId,
                    @Now
                );
            END;

            SET @PageVersionId = NEWID();

            INSERT INTO manga.ChapterPageVersion
            (
                chapter_page_version_id,
                chapter_page_id,
                version_no,
                page_file_id,
                version_note,
                is_current_version
            )
            VALUES
            (
                @PageVersionId,
                @ChapterPageId,
                1,
                @FileResourceId,
                N'SCRUM-116 local seed current page version.',
                1
            );
        END;

        ---------------------------------------------------------------------
        -- Whole-page region on the current page version
        ---------------------------------------------------------------------
        SELECT TOP (1) @PageRegionId = page_region_id
        FROM manga.PageRegion
        WHERE chapter_page_version_id = @PageVersionId
          AND region_label = CONCAT(N'SCRUM-116 Full Page Region ', @PageNo);

        IF @PageRegionId IS NULL
        BEGIN
            SET @PageRegionId = NEWID();

            INSERT INTO manga.PageRegion
            (
                page_region_id,
                chapter_page_version_id,
                type_code,
                region_label,
                x,
                y,
                width,
                height,
                confidence_score,
                source_type,
                original_text,
                created_at_utc,
                created_by_user_id
            )
            VALUES
            (
                @PageRegionId,
                @PageVersionId,
                N'PANEL',
                CONCAT(N'SCRUM-116 Full Page Region ', @PageNo),
                0,
                0,
                100,
                100,
                NULL,
                N'MANUAL',
                NULL,
                @Now,
                @MangakaUserId
            );
        END;

        ---------------------------------------------------------------------
        -- Assistant task.
        -- Note: current DB check constraint does not allow COLORING.
        -- SHADING is used as the closest valid task type for local testing.
        ---------------------------------------------------------------------
        SELECT TOP (1) @TaskId = chapter_page_task_id
        FROM manga.ChapterPageTask
        WHERE assigned_to_user_id = @AssistantUserId
          AND task_title = @TaskTitle
          AND status_code = N'ASSIGNED';

        IF @TaskId IS NULL
        BEGIN
            SET @TaskId = NEWID();

            INSERT INTO manga.ChapterPageTask
            (
                chapter_page_task_id,
                assigned_to_user_id,
                type_code,
                status_code,
                task_title,
                task_description,
                priority_level,
                due_at_utc,
                compensation_amount,
                completed_page_version_id,
                created_at_utc,
                created_by_user_id,
                updated_at_utc
            )
            VALUES
            (
                @TaskId,
                @AssistantUserId,
                N'SHADING',
                N'ASSIGNED',
                @TaskTitle,
                CONCAT(N'Local SCRUM-116 test task for page ', @PageNo, N'. Upload a completed image to test ASSIGNED -> UNDER_REVIEW.'),
                3,
                DATEADD(day, 7, @Now),
                100000.00,
                NULL,
                @Now,
                @MangakaUserId,
                NULL
            );
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM manga.ChapterPageTaskRegion
            WHERE chapter_page_task_id = @TaskId
              AND page_region_id = @PageRegionId
        )
        BEGIN
            INSERT INTO manga.ChapterPageTaskRegion
            (
                chapter_page_task_id,
                page_region_id
            )
            VALUES
            (
                @TaskId,
                @PageRegionId
            );
        END;

        ---------------------------------------------------------------------
        -- Notification
        ---------------------------------------------------------------------
        IF NOT EXISTS
        (
            SELECT 1
            FROM manga.Notification
            WHERE recipient_user_id = @AssistantUserId
              AND notification_type_code = N'TASK_ASSIGNMENT'
              AND related_entity_type = N'ChapterPageTask'
              AND related_entity_id = @TaskId
        )
        BEGIN
            INSERT INTO manga.Notification
            (
                notification_id,
                recipient_user_id,
                notification_type_code,
                title,
                message,
                related_entity_type,
                related_entity_id,
                read_at_utc,
                created_at_utc
            )
            VALUES
            (
                NEWID(),
                @AssistantUserId,
                N'TASK_ASSIGNMENT',
                CONCAT(N'New task assigned: Page ', @PageNo),
                CONCAT(N'You have been assigned a SCRUM-116 local test task for page ', @PageNo, N'.'),
                N'ChapterPageTask',
                @TaskId,
                NULL,
                @Now
            );
        END;

        SET @PageNo += 1;
    END;

    -------------------------------------------------------------------------
    -- Verification output
    -------------------------------------------------------------------------
    SELECT
        DB_NAME() AS current_database,
        @CommitChanges AS commit_changes;

    SELECT
        u.user_id,
        u.username,
        u.display_name,
        u.status_code,
        r.role_name
    FROM auth.Users u
    INNER JOIN auth.Roles r ON r.role_id = u.role_id
    WHERE u.user_id IN (@AssistantUserId, @MangakaUserId)
    ORDER BY r.role_name, u.username;

    SELECT
        series_id,
        title,
        slug,
        status_code,
        publication_frequency_code,
        content_language_code
    FROM manga.Series
    WHERE series_id = @SeriesId;

    SELECT
        chapter_id,
        series_id,
        chapter_number_label,
        chapter_title,
        status_code
    FROM manga.Chapter
    WHERE chapter_id = @ChapterId;

    SELECT
        cp.chapter_page_id,
        cp.chapter_id,
        cp.page_no,
        cpv.chapter_page_version_id,
        cpv.version_no,
        cpv.is_current_version,
        fr.file_resource_id,
        fr.cloudinary_public_id
    FROM manga.ChapterPage cp
    LEFT JOIN manga.ChapterPageVersion cpv ON cpv.chapter_page_id = cp.chapter_page_id
    LEFT JOIN manga.FileResource fr ON fr.file_resource_id = cpv.page_file_id
    WHERE cp.chapter_id = @ChapterId
    ORDER BY cp.page_no, cpv.version_no;

    SELECT
        cp.page_no,
        pr.page_region_id,
        pr.chapter_page_version_id,
        pr.type_code,
        pr.region_label,
        pr.x,
        pr.y,
        pr.width,
        pr.height
    FROM manga.ChapterPage cp
    INNER JOIN manga.ChapterPageVersion cpv ON cpv.chapter_page_id = cp.chapter_page_id
    INNER JOIN manga.PageRegion pr ON pr.chapter_page_version_id = cpv.chapter_page_version_id
    WHERE cp.chapter_id = @ChapterId
      AND pr.region_label LIKE N'SCRUM-116 Full Page Region%'
    ORDER BY cp.page_no;

    SELECT
        cp.page_no,
        t.chapter_page_task_id,
        t.assigned_to_user_id,
        t.type_code,
        t.status_code,
        t.task_title,
        t.priority_level,
        t.due_at_utc,
        t.compensation_amount,
        t.completed_page_version_id
    FROM manga.ChapterPageTask t
    INNER JOIN manga.ChapterPageTaskRegion tr ON tr.chapter_page_task_id = t.chapter_page_task_id
    INNER JOIN manga.PageRegion pr ON pr.page_region_id = tr.page_region_id
    INNER JOIN manga.ChapterPageVersion cpv ON cpv.chapter_page_version_id = pr.chapter_page_version_id
    INNER JOIN manga.ChapterPage cp ON cp.chapter_page_id = cpv.chapter_page_id
    WHERE cp.chapter_id = @ChapterId
      AND t.assigned_to_user_id = @AssistantUserId
      AND t.task_title LIKE N'Coloring Task for Page%'
    ORDER BY cp.page_no;

    SELECT
        n.notification_id,
        n.recipient_user_id,
        n.notification_type_code,
        n.title,
        n.related_entity_type,
        n.related_entity_id,
        n.read_at_utc,
        n.created_at_utc
    FROM manga.Notification n
    WHERE n.recipient_user_id = @AssistantUserId
      AND n.related_entity_type = N'ChapterPageTask'
      AND n.title LIKE N'New task assigned: Page%'
    ORDER BY n.created_at_utc DESC;

    IF @CommitChanges = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'SCRUM-116 assistant test data committed.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'SCRUM-116 assistant test data dry run completed and rolled back. Change @CommitChanges to 1 to commit.';
    END;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity int = ERROR_SEVERITY();
    DECLARE @ErrorState int = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO
