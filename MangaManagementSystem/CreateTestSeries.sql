-- Create test data for SCRUM-116 verification
-- This script creates a pre-approved series with chapters, pages and tasks for assistant user "troly"

-- First, ensure the roles exist
IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE [RoleName] = 'Assistant')
BEGIN
    INSERT INTO [auth].[Roles] ([RoleId], [RoleName])
    VALUES (NEWID(), 'Assistant')
END

IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE [RoleName] = 'Mangaka')
BEGIN
    INSERT INTO [auth].[Roles] ([RoleId], [RoleName])
    VALUES (NEWID(), 'Mangaka')
END

-- Get role IDs
DECLARE @AssistantRoleId UNIQUEIDENTIFIER = (SELECT [RoleId] FROM [auth].[Roles] WHERE [RoleName] = 'Assistant')
DECLARE @MangakaRoleId UNIQUEIDENTIFIER = (SELECT [RoleId] FROM [auth].[Roles] WHERE [RoleName] = 'Mangaka')

-- Create assistant user "troly"
IF NOT EXISTS (SELECT 1 FROM [auth].[Users] WHERE [Username] = 'troly')
BEGIN
    INSERT INTO [auth].[Users] (
        [UserId], [RoleId], [Username], [Email], [DisplayName], 
        [PasswordHash], [StatusCode], [CreatedAtUtc]
    ) VALUES (
        NEWID(), @AssistantRoleId, 'troly', 'troly@example.com', 'Troly Assistant',
        'vJfFZ7YH3Z1qX2w4Y5Z6w7Z8q9Z0q1Z2q3Z4q5Z6w7Z8q9Z0q1Z2q3Z4q5Z6w7Z8q9Z0q1Z2', -- Password123! hashed with SHA256
        'ACTIVE', GETUTCDATE()
    )
END

-- Create mangaka user "khoavq"
IF NOT EXISTS (SELECT 1 FROM [auth].[Users] WHERE [Username] = 'khoavq')
BEGIN
    INSERT INTO [auth].[Users] (
        [UserId], [RoleId], [Username], [Email], [DisplayName], 
        [PasswordHash], [StatusCode], [CreatedAtUtc]
    ) VALUES (
        NEWID(), @MangakaRoleId, 'khoavq', 'khoavq@example.com', 'Khoa VQ Mangaka',
        'vJfFZ7YH3Z1qX2w4Y5Z6w7Z8q9Z0q1Z2q3Z4q5Z6w7Z8q9Z0q1Z2q3Z4q5Z6w7Z8q9Z0q1Z2', -- Password123! hashed with SHA256
        'ACTIVE', GETUTCDATE()
    )
END

-- Get user IDs
DECLARE @TrolyUserId UNIQUEIDENTIFIER = (SELECT [UserId] FROM [auth].[Users] WHERE [Username] = 'troly')
DECLARE @MangakaUserId UNIQUEIDENTIFIER = (SELECT [UserId] FROM [auth].[Users] WHERE [Username] = 'khoavq')

-- Create series
DECLARE @SeriesId UNIQUEIDENTIFIER = NEWID()
INSERT INTO [manga].[Series] (
    [SeriesId], [Title], [Slug], [Synopsis], [Genre], [StatusCode], [ContentLanguageCode], [CreatedAtUtc]
) VALUES (
    @SeriesId, 
    N'Test Series for Troly', 
    'test-series-troly', 
    N'This is a test series created for verifying assistant task functionality.',
    N'Action, Adventure',
    'APPROVED',
    'vi',
    GETUTCDATE()
)

-- Create series contributor
INSERT INTO [manga].[SeriesContributors] (
    [SeriesContributorId], [SeriesId], [UserId], [StartDate]
) VALUES (
    NEWID(), @SeriesId, @MangakaUserId, GETUTCDATE()
)

-- Create chapter
DECLARE @ChapterId UNIQUEIDENTIFIER = NEWID()
INSERT INTO [manga].[Chapters] (
    [ChapterId], [SeriesId], [ChapterNumberLabel], [ChapterTitle], [StatusCode], [CreatedAtUtc], [CreatedByUserId]
) VALUES (
    @ChapterId, @SeriesId, '01', N'Chapter 1: The Beginning', 'APPROVED', GETUTCDATE(), @MangakaUserId
)

-- Create 5 pages
DECLARE @Page1Id UNIQUEIDENTIFIER = NEWID()
DECLARE @Page2Id UNIQUEIDENTIFIER = NEWID()
DECLARE @Page3Id UNIQUEIDENTIFIER = NEWID()
DECLARE @Page4Id UNIQUEIDENTIFIER = NEWID()
DECLARE @Page5Id UNIQUEIDENTIFIER = NEWID()

INSERT INTO [manga].[ChapterPages] ([ChapterPageId], [ChapterId], [PageNo], [PageNotes]) VALUES (@Page1Id, @ChapterId, 1, N'Page 1 notes')
INSERT INTO [manga].[ChapterPages] ([ChapterPageId], [ChapterId], [PageNo], [PageNotes]) VALUES (@Page2Id, @ChapterId, 2, N'Page 2 notes')
INSERT INTO [manga].[ChapterPages] ([ChapterPageId], [ChapterId], [PageNo], [PageNotes]) VALUES (@Page3Id, @ChapterId, 3, N'Page 3 notes')
INSERT INTO [manga].[ChapterPages] ([ChapterPageId], [ChapterId], [PageNo], [PageNotes]) VALUES (@Page4Id, @ChapterId, 4, N'Page 4 notes')
INSERT INTO [manga].[ChapterPages] ([ChapterPageId], [ChapterId], [PageNo], [PageNotes]) VALUES (@Page5Id, @ChapterId, 5, N'Page 5 notes')

-- Create tasks for each page
DECLARE @Task1Id UNIQUEIDENTIFIER = NEWID()
DECLARE @Task2Id UNIQUEIDENTIFIER = NEWID()
DECLARE @Task3Id UNIQUEIDENTIFIER = NEWID()
DECLARE @Task4Id UNIQUEIDENTIFIER = NEWID()
DECLARE @Task5Id UNIQUEIDENTIFIER = NEWID()

INSERT INTO [manga].[ChapterPageTasks] (
    [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], [TaskTitle], [TaskDescription], 
    [PriorityLevel], [DueAtUtc], [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
) VALUES (
    @Task1Id, @TrolyUserId, 'COLORING', 'ASSIGNED', N'Coloring Task for Page 1', 
    N'Please color page 1 of the series. Focus on the background elements.', 
    3, DATEADD(DAY, 7, GETUTCDATE()), 50.00, GETUTCDATE(), @MangakaUserId
)
INSERT INTO [manga].[ChapterPageTasks] (
    [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], [TaskTitle], [TaskDescription], 
    [PriorityLevel], [DueAtUtc], [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
) VALUES (
    @Task2Id, @TrolyUserId, 'COLORING', 'ASSIGNED', N'Coloring Task for Page 2', 
    N'Please color page 2 of the series. Focus on the background elements.', 
    3, DATEADD(DAY, 7, GETUTCDATE()), 50.00, GETUTCDATE(), @MangakaUserId
)
INSERT INTO [manga].[ChapterPageTasks] (
    [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], [TaskTitle], [TaskDescription], 
    [PriorityLevel], [DueAtUtc], [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
) VALUES (
    @Task3Id, @TrolyUserId, 'COLORING', 'ASSIGNED', N'Coloring Task for Page 3', 
    N'Please color page 3 of the series. Focus on the background elements.', 
    3, DATEADD(DAY, 7, GETUTCDATE()), 50.00, GETUTCDATE(), @MangakaUserId
)
INSERT INTO [manga].[ChapterPageTasks] (
    [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], [TaskTitle], [TaskDescription], 
    [PriorityLevel], [DueAtUtc], [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
) VALUES (
    @Task4Id, @TrolyUserId, 'COLORING', 'ASSIGNED', N'Coloring Task for Page 4', 
    N'Please color page 4 of the series. Focus on the background elements.', 
    3, DATEADD(DAY, 7, GETUTCDATE()), 50.00, GETUTCDATE(), @MangakaUserId
)
INSERT INTO [manga].[ChapterPageTasks] (
    [ChapterPageTaskId], [AssignedToUserId], [TypeCode], [StatusCode], [TaskTitle], [TaskDescription], 
    [PriorityLevel], [DueAtUtc], [CompensationAmount], [CreatedAtUtc], [CreatedByUserId]
) VALUES (
    @Task5Id, @TrolyUserId, 'COLORING', 'ASSIGNED', N'Coloring Task for Page 5', 
    N'Please color page 5 of the series. Focus on the background elements.', 
    3, DATEADD(DAY, 7, GETUTCDATE()), 50.00, GETUTCDATE(), @MangakaUserId
)

-- Create notifications
INSERT INTO [manga].[Notifications] (
    [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
    [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
) VALUES (
    NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
    N'You have been assigned a new coloring task: Coloring Task for Page 1',
    'ChapterPageTask', @Task1Id, GETUTCDATE()
)
INSERT INTO [manga].[Notifications] (
    [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
    [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
) VALUES (
    NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
    N'You have been assigned a new coloring task: Coloring Task for Page 2',
    'ChapterPageTask', @Task2Id, GETUTCDATE()
)
INSERT INTO [manga].[Notifications] (
    [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
    [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
) VALUES (
    NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
    N'You have been assigned a new coloring task: Coloring Task for Page 3',
    'ChapterPageTask', @Task3Id, GETUTCDATE()
)
INSERT INTO [manga].[Notifications] (
    [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
    [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
) VALUES (
    NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
    N'You have been assigned a new coloring task: Coloring Task for Page 4',
    'ChapterPageTask', @Task4Id, GETUTCDATE()
)
INSERT INTO [manga].[Notifications] (
    [NotificationId], [RecipientUserId], [NotificationTypeCode], [Title], [Message], 
    [RelatedEntityType], [RelatedEntityId], [CreatedAtUtc]
) VALUES (
    NEWID(), @TrolyUserId, 'NEW_TASK_ASSIGNED', N'New Task Assigned', 
    N'You have been assigned a new coloring task: Coloring Task for Page 5',
    'ChapterPageTask', @Task5Id, GETUTCDATE()
)

-- Display created data summary
SELECT 
    'Series' as EntityType, 
    Title as EntityName, 
    SeriesId as EntityId
FROM [manga].[Series] 
WHERE SeriesId = @SeriesId

SELECT 
    'Chapter' as EntityType, 
    ChapterTitle as EntityName, 
    ChapterId as EntityId
FROM [manga].[Chapters] 
WHERE SeriesId = @SeriesId

SELECT 
    'Page' as EntityType, 
    'Page ' + CAST(PageNo AS VARCHAR) as EntityName, 
    ChapterPageId as EntityId
FROM [manga].[ChapterPages] 
WHERE ChapterId = @ChapterId

SELECT 
    'Task' as EntityType, 
    TaskTitle as EntityName, 
    ChapterPageTaskId as EntityId,
    StatusCode,
    AssignedToUserId
FROM [manga].[ChapterPageTasks] 
WHERE AssignedToUserId = @TrolyUserId

SELECT 
    'Notification' as EntityType, 
    Title as EntityName, 
    NotificationId as EntityId,
    RecipientUserId
FROM [manga].[Notifications] 
WHERE RecipientUserId = @TrolyUserId

SELECT 
    'User' as EntityType, 
    DisplayName as EntityName, 
    UserId as EntityId,
    Username,
    StatusCode
FROM [auth].[Users] 
WHERE Username IN ('troly', 'khoavq')

SELECT 
    'Role' as EntityType, 
    RoleName as EntityName, 
    RoleId as EntityId
FROM [auth].[Roles] 
WHERE RoleName IN ('Assistant', 'Mangaka')
