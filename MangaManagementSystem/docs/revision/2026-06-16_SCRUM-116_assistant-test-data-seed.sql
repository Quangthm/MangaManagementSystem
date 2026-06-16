-- SCRUM-116 Assistant Test Data Seed Script
-- Run this script to create test data for the SCRUM-116 Assistant workflow
-- 
-- Instructions:
-- 1. Run with @CommitChanges = 0 (default) to preview what will be created
-- 2. Review the SELECT verification queries
-- 3. If satisfied, set @CommitChanges = 1 and run again to commit changes
-- 
-- This script is idempotent - it will reuse existing data where possible.

SET NOCOUNT ON;

DECLARE @CommitChanges bit = 0;  -- Set to 1 to commit changes, 0 to rollback (dry run)

BEGIN TRY
    BEGIN TRANSACTION;

    -- ============================================
    -- STEP 1: Locate existing users
    -- ============================================

    -- Check if Assistant user 'troly' exists
    DECLARE @TrolyUserId UNIQUEIDENTIFIER;
    SELECT @TrolyUserId = [UserId] 
    FROM [auth].[Users] 
    WHERE [Username] = 'troly' AND [StatusCode] = 'ACTIVE';

    IF @TrolyUserId IS NULL
    BEGIN
        THROW 50001, 'ERROR: Assistant user "troly" does not exist or is not active. Please create the user first.', 1;
    END

    PRINT 'Found Assistant user: troly (UserId: ' + CAST(@TrolyUserId AS VARCHAR(36)) + ')';

    -- Find an existing Mangaka user
    DECLARE @MangakaUserId UNIQUEIDENTIFIER;

    -- Try khoavq first
    SELECT @MangakaUserId = [UserId] 
    FROM [auth].[Users] 
    WHERE [Username] = 'khoavq' AND [StatusCode] = 'ACTIVE';

    -- If khoavq doesn't exist, find any other active Mangaka user
    IF @MangakaUserId IS NULL
    BEGIN
        SELECT @MangakaUserId = [UserId] 
        FROM [auth].[Users] 
        WHERE [StatusCode] = 'ACTIVE'
          AND [RoleId] = (SELECT [RoleId] FROM [auth].[Roles] WHERE [RoleName] = 'Mangaka')
        ORDER BY [CreatedAtUtc] ASC;  -- Get the earliest created Mangaka
    END

    IF @MangakaUserId IS NULL
    BEGIN
        THROW 50002, 'ERROR: No active Mangaka user found. Please ensure at least one Mangaka user exists.', 1;
    END

    PRINT 'Found Mangaka user: ' + (SELECT [Username] FROM [auth].[Users] WHERE [UserId] = @MangakaUserId) + 
          ' (UserId: ' + CAST(@MangakaUserId AS VARCHAR(36)) + ')';

    -- ============================================
    -- STEP 2: Create or reuse existing Series
    -- ============================================

    DECLARE @SeriesId UNIQUEIDENTIFIER;
    SELECT @SeriesId = [SeriesId] 
    FROM [manga].[Series] 
    WHERE [Title] = N'SCRUM-116 Test Series';

    IF @SeriesId IS NULL
    BEGIN
        SET @SeriesId = NEWID();
        INSERT INTO [manga].[Series] (
            [SeriesId], [Title], [Slug], [Synopsis], [Genre], [StatusCode], 
            [ContentLanguageCode], [CreatedAtUtc]
        ) VALUES (
            @SeriesId, 
            N'SCRUM-116 Test Series', 
            'scrum-116-test-series', 
            N'This is a test series created for verifying SCRUM-116 Assistant task functionality.',
            N'Manga, Action, Adventure',
            'APPROVED',
            'vi',
            GETUTCDATE()
        );
        PRINT 'Created Series: SCRUM-116 Test Series (SeriesId: ' + CAST(@SeriesId AS VARCHAR(36)) + ')';
    END
    ELSE
    BEGIN
        PRINT 'Reused existing Series: SCRUM-116 Test Series (SeriesId: ' + CAST(@SeriesId AS VARCHAR(36)) + ')';
    END

    -- ============================================
    -- STEP 3: Create or reuse existing Chapter
    -- ============================================

    DECLARE @ChapterId UNIQUEIDENTIFIER;
    SELECT @ChapterId = [ChapterId] 
    FROM [manga].[Chapter] 
    WHERE [SeriesId] = @SeriesId AND [ChapterNumberLabel] = '1';

    IF @ChapterId IS NULL
    BEGIN
        SET @ChapterId = NEWID();
        INSERT INTO [manga].[Chapter] (
            [ChapterId], [SeriesId], [ChapterNumberLabel], [ChapterTitle], 
            [StatusCode], [CreatedAtUtc], [CreatedByUserId]
        ) VALUES (
            @ChapterId, 
            @SeriesId, 
            '1', 
            N'SCRUM-116 Test Chapter',
            'APPROVED',
            GETUTCDATE(),
            @MangakaUserId
        );
        PRINT 'Created Chapter 1 for Series (ChapterId: ' + CAST(@ChapterId AS VARCHAR(36)) + ')';
    END
    ELSE
    BEGIN
        PRINT 'Reused existing Chapter 1 (ChapterId: ' + CAST(@ChapterId AS VARCHAR(36)) + ')';
    END

    -- ============================================
    -- STEP 4: Create ChapterPageVersions (required for PageRegions)
    -- ============================================

    -- We need to create ChapterPageVersion records first, then create ChapterPages linked to them
    -- Check if we already have 5 versions for this chapter
    DECLARE @VersionCount INT;
    SELECT @VersionCount = COUNT(*) 
    FROM [manga].[ChapterPageVersion] 
    WHERE [ChapterId] = @ChapterId;

    IF @VersionCount < 5
    BEGIN
        PRINT 'Note: ChapterPageVersion records may need to be created for testing. ' +
              'This script assumes pages exist. If pages are missing, create them manually first.';
    END
    ELSE
    BEGIN
        PRINT 'Found ' + CAST(@VersionCount AS VARCHAR(10)) + ' ChapterPageVersion records for Chapter.';
    END

    -- ============================================
    -- STEP 5: Create ChapterPages if not exists
    -- ============================================

    DECLARE @Page1Id UNIQUEIDENTIFIER, @Page2Id UNIQUEIDENTIFIER, @Page3Id UNIQUEIDENTIFIER;
    DECLARE @Page4Id UNIQUEIDENTIFIER, @Page5Id UNIQUEIDENTIFIER;

    -- Create pages if they don't exist
    SET @Page1Id = (SELECT [ChapterPageId] FROM [manga].[ChapterPage] WHERE [ChapterId] = @ChapterId AND [PageNo] = 1);
    IF @Page1Id IS NULL
    BEGIN
        SET @Page1Id = NEWID();
        INSERT INTO [manga].[ChapterPage] ([ChapterPageId], [ChapterId], [PageNo]) VALUES (@Page1Id, @ChapterId, 1);
    END

    SET @Page2Id = (SELECT [ChapterPageId] FROM [manga].[ChapterPage] WHERE [ChapterId] = @ChapterId AND [PageNo] = 2);
    IF @Page2Id IS NULL
    BEGIN
        SET @Page2Id = NEWID();
        INSERT INTO [manga].[ChapterPage] ([ChapterPageId], [ChapterId], [PageNo]) VALUES (@Page2Id, @ChapterId, 2);
    END

    SET @Page3Id = (SELECT [ChapterPageId] FROM [manga].[ChapterPage] WHERE [ChapterId] = @ChapterId AND [PageNo] = 3);
    IF @Page3Id IS NULL
    BEGIN
        SET @Page3Id = NEWID();
        INSERT INTO [manga].[ChapterPage] ([ChapterPageId], [ChapterId], [PageNo]) VALUES (@Page3Id, @ChapterId, 3);
    END

    SET @Page4Id = (SELECT [ChapterPageId] FROM [manga].[ChapterPage] WHERE [ChapterId] = @ChapterId AND [PageNo] = 4);
    IF @Page4Id IS NULL
    BEGIN
        SET @Page4Id = NEWID();
        INSERT INTO [manga].[ChapterPage] ([ChapterPageId], [ChapterId], [PageNo]) VALUES (@Page4Id, @ChapterId, 4);
    END

    SET @Page5Id = (SELECT [ChapterPageId] FROM [manga].[ChapterPage] WHERE [ChapterId] = @ChapterId AND [PageNo] = 5);
    IF @Page5Id IS NULL
    BEGIN
        SET @Page5Id = NEWID();
        INSERT INTO [manga].[ChapterPage] ([ChapterPageId], [ChapterId], [PageNo]) VALUES (@Page5Id, @ChapterId, 5);
    END

    PRINT 'Pages verified/created for Chapter.';

    -- ============================================
    -- STEP 6: Create PageRegions (if ChapterPageVersion exists)
    -- ============================================

    -- Get the current ChapterPageVersionId for each page
    DECLARE @Page1VersionId UNIQUEIDENTIFIER, @Page2VersionId UNIQUEIDENTIFIER, @Page3VersionId UNIQUEIDENTIFIER;
    DECLARE @Page4VersionId UNIQUEIDENTIFIER, @Page5VersionId UNIQUEIDENTIFIER;

    SELECT @Page1VersionId = [ChapterPageVersionId] FROM [manga].[ChapterPageVersion] WHERE [ChapterPageId] = @Page1Id AND [IsCurrentVersion] = 1;
    SELECT @Page2VersionId = [ChapterPageVersionId] FROM [manga].[ChapterPageVersion] WHERE [ChapterPageId] = @Page2Id AND [IsCurrentVersion] = 1;
    SELECT @Page3VersionId = [ChapterPageVersionId] FROM [manga].[ChapterPageVersion] WHERE [ChapterPageId] = @Page3Id AND [IsCurrentVersion] = 1;
    SELECT @Page4VersionId = [ChapterPageVersionId] FROM [manga].[ChapterPageVersion] WHERE [ChapterPageId] = @Page4Id AND [IsCurrentVersion] = 1;
    SELECT @Page5VersionId = [ChapterPageVersionId] FROM [manga].[ChapterPageVersion] WHERE [ChapterPageId] = @Page5Id AND [IsCurrentVersion] = 1;

    -- Create PageRegions for each page if they don't exist and version exists
    DECLARE @Region1Id UNIQUEIDENTIFIER = NULL, @Region2Id UNIQUEIDENTIFIER = NULL, @Region3Id UNIQUEIDENTIFIER = NULL;
    DECLARE @Region4Id UNIQUEIDENTIFIER = NULL, @Region5Id UNIQUEIDENTIFIER = NULL;

    IF @Page1VersionId IS NOT NULL
    BEGIN
        SELECT @Region1Id = [PageRegionId] FROM [manga].[PageRegion] WHERE [ChapterPageVersionId] = @Page1VersionId;
        IF @Region1Id IS NULL
        BEGIN
            SET @Region1Id = NEWID();
            INSERT INTO [manga].[PageRegion] (
                [PageRegionId], [ChapterPageVersionId], [TypeCode], [SourceType], 
                [X], [Y], [Width], [Height], [ConfidenceScore], [CreatedByUserId]
            ) VALUES (
                @Region1Id, @Page1VersionId, 'OTHER', 'MANUAL',
                0.00, 0.00, 100.00, 100.00, 1.0000, @MangakaUserId
            );
        END
    END

    IF @Page2VersionId IS NOT NULL
    BEGIN
        SELECT @Region2Id = [PageRegionId] FROM [manga].[PageRegion] WHERE [ChapterPageVersionId] = @Page2VersionId;
        IF @Region2Id IS NULL
        BEGIN
            SET @Region2Id = NEWID();
            INSERT INTO [manga].[PageRegion] (
                [PageRegionId], [ChapterPageVersionId], [TypeCode], [SourceType], 
                [X], [Y], [Width], [Height], [ConfidenceScore], [CreatedByUserId]
            ) VALUES (
                @Region2Id, @Page2VersionId, 'OTHER', 'MANUAL',
                0.00, 0.00, 100.00, 100.00, 1.0000, @MangakaUserId
            );
        END
    END

    IF @Page3VersionId IS NOT NULL
    BEGIN
        SELECT @Region3Id = [PageRegionId] FROM [manga].[PageRegion] WHERE [ChapterPageVersionId] = @Page3VersionId;
        IF @Region3Id IS NULL
        BEGIN
            SET @Region3Id = NEWID();
            INSERT INTO [manga].[PageRegion] (
                [PageRegionId], [ChapterPageVersionId], [TypeCode], [SourceType], 
                [X], [Y], [Width], [Height], [ConfidenceScore], [CreatedByUserId]
            ) VALUES (
                @Region3Id, @Page3VersionId, 'OTHER', 'MANUAL',
                0.00, 0.00, 100.00, 100.00, 1.0000, @MangakaUserId
            );
        END
    END

    IF @Page4VersionId IS NOT NULL
    BEGIN
        SELECT @Region4Id = [PageRegionId] FROM [manga].[PageRegion] WHERE [ChapterPageVersionId] = @Page4VersionId;
        IF @Region4Id IS NULL
        BEGIN
            SET @Region4Id = NEWID();
            INSERT INTO [manga].[PageRegion] (
                [PageRegionId], [ChapterPageVersionId], [TypeCode], [SourceType], 
                [X], [Y], [Width], [Height], [ConfidenceScore], [CreatedByUserId]
            ) VALUES (
                @Region4Id, @Page4VersionId, 'OTHER', 'MANUAL',
                0.00, 0.00, 100.00, 100.00, 1.0000, @MangakaUserId
            );
        END
    END

    IF @Page5VersionId IS NOT NULL
    BEGIN
        SELECT @Region5Id = [PageRegionId] FROM [manga].[PageRegion] WHERE [ChapterPageVersionId] = @Page5VersionId;
        IF @Region5Id IS NULL
        BEGIN
            SET @Region5Id = NEWID();
            INSERT INTO [manga].[PageRegion] (
                [PageRegionId], [ChapterPageVersionId], [TypeCode], [SourceType], 
                [X], [Y], [Width], [Height], [ConfidenceScore], [CreatedByUserId]
            ) VALUES (
                @Region5Id, @Page5VersionId, 'OTHER', 'MANUAL',
                0.00, 0.00, 100.00, 100.00, 1.0000, @MangakaUserId
            );
        END
    END

    PRINT 'PageRegions created/verified (requires ChapterPageVersion to exist first).';

    -- ============================================
    -- STEP 7: Create ChapterPageTasks
    -- ============================================

    DECLARE @Task1Id UNIQUEIDENTIFIER, @Task2Id UNIQUEIDENTIFIER, @Task3Id UNIQUEIDENTIFIER;
    DECLARE @Task4Id UNIQUEIDENTIFIER, @Task5Id UNIQUEIDENTIFIER;

    SET @Task1Id = (SELECT [ChapterPageTaskId] FROM [manga].[ChapterPageTask] WHERE [AssignedToUserId] = @TrolyUserId AND [TaskTitle] = N'Coloring Task for Page 1');
    IF @Task1Id IS NULL AND @Region1Id IS NOT NULL
    BEGIN
        SET @Task1Id = NEWID();
        INSERT INTO [manga].[ChapterPageTask] (
            [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], 
            [TaskTitle], [TaskDescription], [PriorityLevel], [DueAtUtc], 
            [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
        ) VALUES (
            @Task1Id, @TrolyUserId, 'COLORING', 'ASSIGNED',
            N'Coloring Task for Page 1',
            N'Please color page 1 of the series. Focus on the background elements.',
            3, DATEADD(DAY, 7, GETUTCDATE()),
            50.00, GETUTCDATE(), @MangakaUserId
        );

        -- Link task to page region
        INSERT INTO [manga].[ChapterPageTaskRegion] ([ChapterPageTaskId], [PageRegionId])
        VALUES (@Task1Id, @Region1Id);
    END

    SET @Task2Id = (SELECT [ChapterPageTaskId] FROM [manga].[ChapterPageTask] WHERE [AssignedToUserId] = @TrolyUserId AND [TaskTitle] = N'Coloring Task for Page 2');
    IF @Task2Id IS NULL AND @Region2Id IS NOT NULL
    BEGIN
        SET @Task2Id = NEWID();
        INSERT INTO [manga].[ChapterPageTask] (
            [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], 
            [TaskTitle], [TaskDescription], [PriorityLevel], [DueAtUtc], 
            [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
        ) VALUES (
            @Task2Id, @TrolyUserId, 'COLORING', 'ASSIGNED',
            N'Coloring Task for Page 2',
            N'Please color page 2 of the series. Focus on the background elements.',
            3, DATEADD(DAY, 7, GETUTCDATE()),
            50.00, GETUTCDATE(), @MangakaUserId
        );

        -- Link task to page region
        INSERT INTO [manga].[ChapterPageTaskRegion] ([ChapterPageTaskId], [PageRegionId])
        VALUES (@Task2Id, @Region2Id);
    END

    SET @Task3Id = (SELECT [ChapterPageTaskId] FROM [manga].[ChapterPageTask] WHERE [AssignedToUserId] = @TrolyUserId AND [TaskTitle] = N'Coloring Task for Page 3');
    IF @Task3Id IS NULL AND @Region3Id IS NOT NULL
    BEGIN
        SET @Task3Id = NEWID();
        INSERT INTO [manga].[ChapterPageTask] (
            [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], 
            [TaskTitle], [TaskDescription], [PriorityLevel], [DueAtUtc], 
            [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
        ) VALUES (
            @Task3Id, @TrolyUserId, 'COLORING', 'ASSIGNED',
            N'Coloring Task for Page 3',
            N'Please color page 3 of the series. Focus on the background elements.',
            3, DATEADD(DAY, 7, GETUTCDATE()),
            50.00, GETUTCDATE(), @MangakaUserId
        );

        -- Link task to page region
        INSERT INTO [manga].[ChapterPageTaskRegion] ([ChapterPageTaskId], [PageRegionId])
        VALUES (@Task3Id, @Region3Id);
    END

    SET @Task4Id = (SELECT [ChapterPageTaskId] FROM [manga].[ChapterPageTask] WHERE [AssignedToUserId] = @TrolyUserId AND [TaskTitle] = N'Coloring Task for Page 4');
    IF @Task4Id IS NULL AND @Region4Id IS NOT NULL
    BEGIN
        SET @Task4Id = NEWID();
        INSERT INTO [manga].[ChapterPageTask] (
            [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], 
            [TaskTitle], [TaskDescription], [PriorityLevel], [DueAtUtc], 
            [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
        ) VALUES (
            @Task4Id, @TrolyUserId, 'COLORING', 'ASSIGNED',
            N'Coloring Task for Page 4',
            N'Please color page 4 of the series. Focus on the background elements.',
            3, DATEADD(DAY, 7, GETUTCDATE()),
            50.00, GETUTCDATE(), @MangakaUserId
        );

        -- Link task to page region
        INSERT INTO [manga].[ChapterPageTaskRegion] ([ChapterPageTaskId], [PageRegionId])
        VALUES (@Task4Id, @Region4Id);
    END

    SET @Task5Id = (SELECT [ChapterPageTaskId] FROM [manga].[ChapterPageTask] WHERE [AssignedToUserId] = @TrolyUserId AND [TaskTitle] = N'Coloring Task for Page 5');
    IF @Task5Id IS NULL AND @Region5Id IS NOT NULL
    BEGIN
        SET @Task5Id = NEWID();
        INSERT INTO [manga].[ChapterPageTask] (
            [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], 
            [TaskTitle], [TaskDescription], [PriorityLevel], [DueAtUtc], 
            [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
        ) VALUES (
            @Task5Id, @TrolyUserId, 'COLORING', 'ASSIGNED',
            N'Coloring Task for Page 5',
            N'Please color page 5 of the series. Focus on the background elements.',
            3, DATEADD(DAY, 7, GETUTCDATE()),
            50.00, GETUTCDATE(), @MangakaUserId
        );

        -- Link task to page region
        INSERT INTO [manga].[ChapterPageTaskRegion] ([ChapterPageTaskId], [PageRegionId])
        VALUES (@Task5Id, @Region5Id);
    END

    PRINT 'ChapterPageTasks created/verified.';

    -- ============================================
    -- STEP 8: Create Notifications (if exists)
    -- ============================================

    -- Check if notifications table has data
    IF EXISTS (SELECT 1 FROM [manga].[Notification] WHERE [RecipientUserId] = @TrolyUserId AND [RelatedEntityId] = @Task1Id)
    BEGIN
        PRINT 'Notifications already exist for these tasks.';
    END
    ELSE IF EXISTS (SELECT 1 FROM [manga].[Notification])
    BEGIN
        -- Create notifications for each task if they don't exist
        IF NOT EXISTS (SELECT 1 FROM [manga].[Notification] WHERE [RecipientUserId] = @TrolyUserId AND [RelatedEntityId] = @Task1Id)
        BEGIN
            INSERT INTO [manga].[Notification] (
                [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
                [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
            ) VALUES (
                NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
                N'You have been assigned a new coloring task: Coloring Task for Page 1',
                'ChapterPageTask', @Task1Id, GETUTCDATE()
            );
        END

        IF NOT EXISTS (SELECT 1 FROM [manga].[Notification] WHERE [RecipientUserId] = @TrolyUserId AND [RelatedEntityId] = @Task2Id)
        BEGIN
            INSERT INTO [manga].[Notification] (
                [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
                [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
            ) VALUES (
                NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
                N'You have been assigned a new coloring task: Coloring Task for Page 2',
                'ChapterPageTask', @Task2Id, GETUTCDATE()
            );
        END

        IF NOT EXISTS (SELECT 1 FROM [manga].[Notification] WHERE [RecipientUserId] = @TrolyUserId AND [RelatedEntityId] = @Task3Id)
        BEGIN
            INSERT INTO [manga].[Notification] (
                [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
                [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
            ) VALUES (
                NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
                N'You have been assigned a new coloring task: Coloring Task for Page 3',
                'ChapterPageTask', @Task3Id, GETUTCDATE()
            );
        END

        IF NOT EXISTS (SELECT 1 FROM [manga].[Notification] WHERE [RecipientUserId] = @TrolyUserId AND [RelatedEntityId] = @Task4Id)
        BEGIN
            INSERT INTO [manga].[Notification] (
                [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
                [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
            ) VALUES (
                NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
                N'You have been assigned a new coloring task: Coloring Task for Page 4',
                'ChapterPageTask', @Task4Id, GETUTCDATE()
            );
        END

        IF NOT EXISTS (SELECT 1 FROM [manga].[Notification] WHERE [RecipientUserId] = @TrolyUserId AND [RelatedEntityId] = @Task5Id)
        BEGIN
            INSERT INTO [manga].[Notification] (
                [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
                [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
            ) VALUES (
                NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
                N'You have been assigned a new coloring task: Coloring Task for Page 5',
                'ChapterPageTask', @Task5Id, GETUTCDATE()
            );
        END

        PRINT 'Notifications created.';
    END
    ELSE
    BEGIN
        PRINT 'Notifications table exists but is empty. Notifications not created.';
    END

    -- ============================================
    -- VERIFICATION QUERIES (Always run, regardless of @CommitChanges)
    -- ============================================

    PRINT '';
    PRINT '=== VERIFICATION QUERIES ===';
    PRINT '';

    PRINT '1. Assistant User (troly):';
    SELECT [UserId], [Username], [Email], [DisplayName], [StatusCode], [RoleId]
    FROM [auth].[Users] 
    WHERE [UserId] = @TrolyUserId;

    PRINT '';
    PRINT '2. Mangaka User:';
    SELECT [UserId], [Username], [Email], [DisplayName], [StatusCode], [RoleId]
    FROM [auth].[Users] 
    WHERE [UserId] = @MangakaUserId;

    PRINT '';
    PRINT '3. Series:';
    SELECT [SeriesId], [Title], [Slug], [StatusCode], [ContentLanguageCode], [CreatedAtUtc]
    FROM [manga].[Series] 
    WHERE [SeriesId] = @SeriesId;

    PRINT '';
    PRINT '4. Chapter:';
    SELECT [ChapterId], [SeriesId], [ChapterNumberLabel], [ChapterTitle], [StatusCode], [CreatedAtUtc]
    FROM [manga].[Chapter] 
    WHERE [ChapterId] = @ChapterId;

    PRINT '';
    PRINT '5. Pages (ChapterPages):';
    SELECT [ChapterPageId], [ChapterId], [PageNo]
    FROM [manga].[ChapterPage] 
    WHERE [ChapterId] = @ChapterId
    ORDER BY [PageNo];

    PRINT '';
    PRINT '6. PageRegions (if ChapterPageVersion exists):';
    IF @Page1VersionId IS NOT NULL
        SELECT [PageRegionId], [ChapterPageVersionId], [TypeCode], [X], [Y], [Width], [Height]
        FROM [manga].[PageRegion] 
        WHERE [ChapterPageVersionId] = @Page1VersionId;
    ELSE
        PRINT 'ChapterPageVersion not found for Page 1. Create ChapterPageVersion records first.';

    PRINT '';
    PRINT '7. ChapterPageTasks assigned to troly:';
    SELECT [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], 
           [TaskTitle], [TaskDescription], [PriorityLevel], [DueAtUtc], [CompensationAmount]
    FROM [manga].[ChapterPageTask] 
    WHERE [AssignedToUserId] = @TrolyUserId
    ORDER BY [TaskTitle];

    PRINT '';
    PRINT '8. Task-Region Links:';
    SELECT tr.[ChapterPageTaskId], tr.[PageRegionId], ct.[TaskTitle], pr.[PageRegionId] as [RegionId]
    FROM [manga].[ChapterPageTaskRegion] tr
    INNER JOIN [manga].[ChapterPageTask] ct ON tr.[ChapterPageTaskId] = ct.[ChapterPageTaskId]
    LEFT JOIN [manga].[PageRegion] pr ON tr.[PageRegionId] = pr.[PageRegionId]
    WHERE tr.[ChapterPageTaskId] IN (@Task1Id, @Task2Id, @Task3Id, @Task4Id, @Task5Id);

    PRINT '';
    PRINT '9. Notifications for troly:';
    SELECT [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
           [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc], [ReadAtUtc]
    FROM [manga].[Notification] 
    WHERE [RecipientUserId] = @TrolyUserId
    ORDER BY [CreatedAtUtc] DESC;

    PRINT '';
    PRINT '=== END OF VERIFICATION ===';
    PRINT '';

    -- ============================================
    -- COMMIT OR ROLLBACK
    -- ============================================

    IF @CommitChanges = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'COMMIT: All changes have been committed successfully.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'ROLLBACK: No changes were committed. This was a dry run.';
        PRINT 'To commit changes, set @CommitChanges = 1 and run the script again.';
    END

END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT '';
    PRINT '=== ERROR ===';
    PRINT 'Error: ' + @ErrorMessage;
    PRINT '';
    PRINT 'ROLLBACK: Transaction was rolled back due to error.';
    PRINT '';

    THROW @ErrorSeverity, @ErrorMessage, @ErrorState;
END CATCH;

-- ============================================
-- MANUAL TEST INSTRUCTIONS
-- ============================================
PRINT '';
PRINT '=== MANUAL TEST INSTRUCTIONS ===';
PRINT '';
PRINT 'After running this script with @CommitChanges = 1, perform the following tests:';
PRINT '';
PRINT '1. Log in as user "troly" with password "Password123!"';
PRINT '   - Username: troly';
PRINT '   - Password: Password123!';
PRINT '';
PRINT '2. Navigate to /assistant/tasks - verify you see 5 tasks assigned to you';
PRINT '';
PRINT '3. Check for notifications';
PRINT '   - Verify you see 5 notifications about new tasks';
PRINT '';
PRINT '4. Verify task details';
PRINT '   - Task titles should be: "Coloring Task for Page 1" through "Coloring Task for Page 5"';
PRINT '   - Task status should be "ASSIGNED"';
PRINT '   - Due date should be 7 days from today';
PRINT '   - Compensation amount should be 50.00';
PRINT '';
PRINT '5. If pages exist with ChapterPageVersions:';
PRINT '   - Each task should be linked to a PageRegion';
PRINT '   - The task should show the page context correctly';
PRINT '';
PRINT '6. Test notification preferences and settings if implemented';
PRINT '   - Navigate to notification/settings pages';
PRINT '   - Verify UI renders correctly';
PRINT '';
PRINT '=== END OF INSTRUCTIONS ===';
