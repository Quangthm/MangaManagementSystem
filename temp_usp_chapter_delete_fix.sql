SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE manga.usp_Chapter_Delete
    @chapter_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @started_tran BIT = 0;

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @started_tran = 1;
            BEGIN TRAN;
        END;

        -- 1. Get all Chapter Pages
        DECLARE @Pages TABLE (chapter_page_id UNIQUEIDENTIFIER);
        INSERT INTO @Pages SELECT chapter_page_id FROM manga.ChapterPage WHERE chapter_id = @chapter_id;

        -- 2. Get all Page Versions
        DECLARE @Versions TABLE (chapter_page_version_id UNIQUEIDENTIFIER);
        INSERT INTO @Versions SELECT chapter_page_version_id FROM manga.ChapterPageVersion WHERE chapter_page_id IN (SELECT chapter_page_id FROM @Pages);

        -- 3. Get all Page Regions
        DECLARE @Regions TABLE (page_region_id UNIQUEIDENTIFIER);
        INSERT INTO @Regions SELECT page_region_id FROM manga.PageRegion WHERE chapter_page_version_id IN (SELECT chapter_page_version_id FROM @Versions);

        -- 4. Get all Tasks linked to these regions
        DECLARE @Tasks TABLE (chapter_page_task_id UNIQUEIDENTIFIER);
        INSERT INTO @Tasks SELECT DISTINCT chapter_page_task_id FROM manga.ChapterPageTaskRegion WHERE page_region_id IN (SELECT page_region_id FROM @Regions);

        -- 5. Get all Annotations linked to these regions
        DECLARE @Annotations TABLE (chapter_page_annotation_id UNIQUEIDENTIFIER);
        INSERT INTO @Annotations SELECT DISTINCT chapter_page_annotation_id FROM manga.ChapterPageAnnotationRegion WHERE page_region_id IN (SELECT page_region_id FROM @Regions);

        -- 6. Delete Task Regions
        DELETE FROM manga.ChapterPageTaskRegion WHERE page_region_id IN (SELECT page_region_id FROM @Regions);

        -- 7. Delete Tasks
        DELETE FROM manga.ChapterPageTask WHERE chapter_page_task_id IN (SELECT chapter_page_task_id FROM @Tasks);

        -- 8. Delete Annotation Regions
        DELETE FROM manga.ChapterPageAnnotationRegion WHERE page_region_id IN (SELECT page_region_id FROM @Regions);

        -- 9. Delete Annotations
        DELETE FROM manga.ChapterPageAnnotation WHERE chapter_page_annotation_id IN (SELECT chapter_page_annotation_id FROM @Annotations);

        -- 10. Delete Page Regions
        DELETE FROM manga.PageRegion WHERE page_region_id IN (SELECT page_region_id FROM @Regions);

        -- 11. Delete Page Versions
        DELETE FROM manga.ChapterPageVersion WHERE chapter_page_version_id IN (SELECT chapter_page_version_id FROM @Versions);

        -- 12. Delete Chapter Pages
        DELETE FROM manga.ChapterPage WHERE chapter_page_id IN (SELECT chapter_page_id FROM @Pages);

        -- 13. Delete Editorial Reviews
        DELETE FROM manga.ChapterEditorialReview WHERE chapter_id = @chapter_id;

        -- 14. Delete Reader Vote Snapshots
        DELETE FROM manga.ChapterReaderVoteSnapshot WHERE chapter_id = @chapter_id;

        -- 15. Finally, Delete the Chapter
        DELETE FROM manga.Chapter WHERE chapter_id = @chapter_id;

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
