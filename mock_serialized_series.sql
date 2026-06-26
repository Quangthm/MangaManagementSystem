/*
    Mock serialized series data for MangaManagementSystem app testing.

    What this creates:
    - 1 SERIALIZED series
    - Normalized Genre/SeriesGenre links resolved from real seeded Genre rows
    - Normalized Tag/SeriesTag links resolved from real seeded Tag rows
    - Series contributors: Mangaka, Assistant, Tantou Editor
    - 2 chapters
    - 3 pages
    - ChapterPageVersion rows with CHAPTER_PAGE_VERSION FileResource rows
    - PageRegion rows
    - ChapterPageTask rows linked through ChapterPageTaskRegion
    - ChapterPageAnnotation rows linked through ChapterPageAnnotationRegion

    Replace the user IDs below if you do not use the TestMangaka/TestAssistant/TestEditor seed accounts.
*/
USE [MangaManagementDB];
GO
DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

----------------------------------------------------------------------
-- QUICK REPLACE AREA
-- Replace these NULL values with real user_id GUIDs when needed.
-- If left NULL, the script tries to resolve your earlier test users.
----------------------------------------------------------------------

DECLARE @MangakaUserId UNIQUEIDENTIFIER = NULL;
DECLARE @AssistantUserId UNIQUEIDENTIFIER = NULL;
DECLARE @EditorUserId UNIQUEIDENTIFIER = NULL;

-- For mock data portability, use names here and resolve them to the real
-- DB-generated Genre/Tag GUIDs before inserting SeriesGenre/SeriesTag links.
-- These names must already exist in manga.Genre and manga.Tag from the base lookup seed.
DECLARE @GenreNamesJson NVARCHAR(MAX) = N'["Action","Fantasy"]';
DECLARE @TagNamesJson NVARCHAR(MAX) = N'["Original Work","Magic","Found Family"]';

-- Example manual replacement:
-- SET @MangakaUserId = 'PUT-MANGAKA-USER-GUID-HERE';
-- SET @AssistantUserId = 'PUT-ASSISTANT-USER-GUID-HERE';
-- SET @EditorUserId = 'PUT-EDITOR-USER-GUID-HERE';

IF @MangakaUserId IS NULL
BEGIN
    SELECT TOP (1)
        @MangakaUserId = u.user_id
    FROM auth.Users u
    WHERE u.username = N'TestMangaka1';
END;

IF @AssistantUserId IS NULL
BEGIN
    SELECT TOP (1)
        @AssistantUserId = u.user_id
    FROM auth.Users u
    WHERE u.username = N'TestAssistant1';
END;

IF @EditorUserId IS NULL
BEGIN
    SELECT TOP (1)
        @EditorUserId = u.user_id
    FROM auth.Users u
    WHERE u.username = N'TestEditor1';
END;

IF @MangakaUserId IS NULL
BEGIN
    ;THROW 59901, 'Mangaka user id was not found. Replace @MangakaUserId with a real auth.Users.user_id.', 1;
END;

IF @AssistantUserId IS NULL
BEGIN
    ;THROW 59902, 'Assistant user id was not found. Replace @AssistantUserId with a real auth.Users.user_id.', 1;
END;

IF @EditorUserId IS NULL
BEGIN
    ;THROW 59903, 'Editor user id was not found. Replace @EditorUserId with a real auth.Users.user_id.', 1;
END;

----------------------------------------------------------------------
-- Fixed mock IDs
----------------------------------------------------------------------

DECLARE @SeriesId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @SeriesCoverFileId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000101';

DECLARE @Chapter1Id UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @Chapter2Id UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';

DECLARE @Chapter1Page1Id UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @Chapter1Page2Id UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000002';
DECLARE @Chapter2Page1Id UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000003';

DECLARE @Chapter1Page1FileId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000001';
DECLARE @Chapter1Page2FileId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000002';
DECLARE @Chapter2Page1FileId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000003';
DECLARE @Chapter1Page2OutputFileId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000004';

DECLARE @Chapter1Page1VersionId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
DECLARE @Chapter1Page2Version1Id UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000002';
DECLARE @Chapter2Page1VersionId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000003';
DECLARE @Chapter1Page2Version2Id UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000004';

DECLARE @Chapter1Page1PanelRegionId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000001';
DECLARE @Chapter1Page1BubbleRegionId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000002';
DECLARE @Chapter1Page2PanelRegionId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000003';
DECLARE @Chapter2Page1PanelRegionId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000004';

DECLARE @Task1Id UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000001';
DECLARE @Task2Id UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000002';

DECLARE @Annotation1Id UNIQUEIDENTIFIER = '80000000-0000-0000-0000-000000000001';
DECLARE @Annotation2Id UNIQUEIDENTIFIER = '80000000-0000-0000-0000-000000000002';

----------------------------------------------------------------------
-- FileResource rows
----------------------------------------------------------------------

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
SELECT
    v.file_resource_id,
    v.file_purpose_code,
    v.original_file_name,
    v.cloudinary_public_id,
    v.cloudinary_secure_url,
    v.content_type,
    v.file_size_bytes,
    v.sha256_hash,
    v.uploaded_by_user_id,
    @Now
FROM
(
    VALUES
    (
        @SeriesCoverFileId,
        N'SERIES_COVER',
        N'mock-series-cover.png',
        N'mock/manga-flow/series/mock-serialized-series/cover',
        N'https://res.cloudinary.com/demo/image/upload/mock/manga-flow/series/mock-serialized-series/cover.png',
        N'image/png',
        CONVERT(BIGINT, 250000),
        N'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
        @MangakaUserId
    ),
    (
        @Chapter1Page1FileId,
        N'CHAPTER_PAGE_VERSION',
        N'mock-chapter-1-page-1.png',
        N'mock/manga-flow/series/mock-serialized-series/chapter-1/page-1-v1',
        N'https://res.cloudinary.com/demo/image/upload/mock/manga-flow/series/mock-serialized-series/chapter-1/page-1-v1.png',
        N'image/png',
        CONVERT(BIGINT, 350000),
        N'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
        @MangakaUserId
    ),
    (
        @Chapter1Page2FileId,
        N'CHAPTER_PAGE_VERSION',
        N'mock-chapter-1-page-2.png',
        N'mock/manga-flow/series/mock-serialized-series/chapter-1/page-2-v1',
        N'https://res.cloudinary.com/demo/image/upload/mock/manga-flow/series/mock-serialized-series/chapter-1/page-2-v1.png',
        N'image/png',
        CONVERT(BIGINT, 360000),
        N'cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc',
        @MangakaUserId
    ),
    (
        @Chapter2Page1FileId,
        N'CHAPTER_PAGE_VERSION',
        N'mock-chapter-2-page-1.png',
        N'mock/manga-flow/series/mock-serialized-series/chapter-2/page-1-v1',
        N'https://res.cloudinary.com/demo/image/upload/mock/manga-flow/series/mock-serialized-series/chapter-2/page-1-v1.png',
        N'image/png',
        CONVERT(BIGINT, 370000),
        N'dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd',
        @MangakaUserId
    ),
    (
        @Chapter1Page2OutputFileId,
        N'CHAPTER_PAGE_VERSION',
        N'mock-chapter-1-page-2-output-v2.png',
        N'mock/manga-flow/series/mock-serialized-series/chapter-1/page-2-v2-output',
        N'https://res.cloudinary.com/demo/image/upload/mock/manga-flow/series/mock-serialized-series/chapter-1/page-2-v2-output.png',
        N'image/png',
        CONVERT(BIGINT, 380000),
        N'eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee',
        @AssistantUserId
    )
) AS v
(
    file_resource_id,
    file_purpose_code,
    original_file_name,
    cloudinary_public_id,
    cloudinary_secure_url,
    content_type,
    file_size_bytes,
    sha256_hash,
    uploaded_by_user_id
)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.FileResource fr
    WHERE fr.file_resource_id = v.file_resource_id
       OR fr.cloudinary_public_id = v.cloudinary_public_id
);

----------------------------------------------------------------------
-- Serialized series
----------------------------------------------------------------------

INSERT INTO manga.Series
(
    series_id,
    title,
    slug,
    synopsis,
    cover_file_id,
    status_code,
    content_language_code,
    created_at_utc,
    publication_frequency_code
)
SELECT
    @SeriesId,
    N'Mock Serialized Series',
    N'mock-serialized-series',
    N'A mock serialized manga series used for dashboard, chapter, page, task, and annotation testing.',
    @SeriesCoverFileId,
    N'SERIALIZED',
    N'ja',
    @Now,
    N'WEEKLY'
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.Series s
    WHERE s.series_id = @SeriesId
       OR s.slug = N'mock-serialized-series'
);

----------------------------------------------------------------------
-- Normalized genre/tag resolution
-- The base Genre/Tag lookup seed uses DB-generated GUIDs. This mock
-- script therefore accepts names, resolves the real IDs, and writes
-- SeriesGenre/SeriesTag using those actual IDs.
----------------------------------------------------------------------

DECLARE @SelectedGenreIds TABLE
(
    genre_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
);

DECLARE @SelectedTagIds TABLE
(
    tag_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
);

IF ISJSON(@GenreNamesJson) <> 1 OR LEFT(LTRIM(@GenreNamesJson), 1) <> N'['
BEGIN
    ;THROW 59904, '@GenreNamesJson must be a JSON array of genre names, for example ["Action","Fantasy"].', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM OPENJSON(@GenreNamesJson)
    WHERE [type] <> 1
)
BEGIN
    ;THROW 59905, '@GenreNamesJson must contain string genre names only.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM OPENJSON(@GenreNamesJson)
    WHERE NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), [value]))), N'') IS NULL
)
BEGIN
    ;THROW 59906, '@GenreNamesJson must not contain empty genre names.', 1;
END;

DECLARE @MissingGenreNames NVARCHAR(MAX);

;WITH RequestedGenreNames AS
(
    SELECT DISTINCT
        genre_name = LTRIM(RTRIM(CONVERT(NVARCHAR(100), j.[value])))
    FROM OPENJSON(@GenreNamesJson) j
)
SELECT
    @MissingGenreNames = STRING_AGG(r.genre_name, N', ')
FROM RequestedGenreNames r
LEFT JOIN manga.Genre g
    ON g.genre_name = r.genre_name
WHERE g.genre_id IS NULL;

IF @MissingGenreNames IS NOT NULL
BEGIN
    DECLARE @MissingGenreMessage NVARCHAR(2048) =
        N'The following genre names were not found in manga.Genre. Run the base genre lookup seed first or correct @GenreNamesJson: '
        + @MissingGenreNames;

    ;THROW 59907, @MissingGenreMessage, 1;
END;

INSERT INTO @SelectedGenreIds
(
    genre_id
)
SELECT DISTINCT
    g.genre_id
FROM OPENJSON(@GenreNamesJson) j
INNER JOIN manga.Genre g
    ON g.genre_name = LTRIM(RTRIM(CONVERT(NVARCHAR(100), j.[value])));

IF NOT EXISTS
(
    SELECT 1
    FROM @SelectedGenreIds
)
BEGIN
    ;THROW 59908, 'At least one valid genre is required for the mock series.', 1;
END;

IF ISJSON(@TagNamesJson) <> 1 OR LEFT(LTRIM(@TagNamesJson), 1) <> N'['
BEGIN
    ;THROW 59909, '@TagNamesJson must be a JSON array of tag names, for example ["Original Work","Magic"].', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM OPENJSON(@TagNamesJson)
    WHERE [type] <> 1
)
BEGIN
    ;THROW 59910, '@TagNamesJson must contain string tag names only.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM OPENJSON(@TagNamesJson)
    WHERE NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(100), [value]))), N'') IS NULL
)
BEGIN
    ;THROW 59911, '@TagNamesJson must not contain empty tag names.', 1;
END;

DECLARE @MissingTagNames NVARCHAR(MAX);

;WITH RequestedTagNames AS
(
    SELECT DISTINCT
        tag_name = LTRIM(RTRIM(CONVERT(NVARCHAR(100), j.[value])))
    FROM OPENJSON(@TagNamesJson) j
)
SELECT
    @MissingTagNames = STRING_AGG(r.tag_name, N', ')
FROM RequestedTagNames r
LEFT JOIN manga.Tag t
    ON t.tag_name = r.tag_name
WHERE t.tag_id IS NULL;

IF @MissingTagNames IS NOT NULL
BEGIN
    DECLARE @MissingTagMessage NVARCHAR(2048) =
        N'The following tag names were not found in manga.Tag. Run the base tag lookup seed first or correct @TagNamesJson: '
        + @MissingTagNames;

    ;THROW 59912, @MissingTagMessage, 1;
END;

INSERT INTO @SelectedTagIds
(
    tag_id
)
SELECT DISTINCT
    t.tag_id
FROM OPENJSON(@TagNamesJson) j
INNER JOIN manga.Tag t
    ON t.tag_name = LTRIM(RTRIM(CONVERT(NVARCHAR(100), j.[value])));

----------------------------------------------------------------------
-- Series-genre links
-- Replace this mock series' genre links with the selected real lookup IDs.
----------------------------------------------------------------------

DELETE sg
FROM manga.SeriesGenre sg
WHERE sg.series_id = @SeriesId
  AND NOT EXISTS
  (
      SELECT 1
      FROM @SelectedGenreIds selected
      WHERE selected.genre_id = sg.genre_id
  );

INSERT INTO manga.SeriesGenre
(
    series_id,
    genre_id
)
SELECT
    @SeriesId,
    selected.genre_id
FROM @SelectedGenreIds selected
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.SeriesGenre sg
    WHERE sg.series_id = @SeriesId
      AND sg.genre_id = selected.genre_id
);

----------------------------------------------------------------------
-- Series-tag links
-- Replace this mock series' tag links with the selected real lookup IDs.
----------------------------------------------------------------------

DELETE st
FROM manga.SeriesTag st
WHERE st.series_id = @SeriesId
  AND NOT EXISTS
  (
      SELECT 1
      FROM @SelectedTagIds selected
      WHERE selected.tag_id = st.tag_id
  );

INSERT INTO manga.SeriesTag
(
    series_id,
    tag_id
)
SELECT
    @SeriesId,
    selected.tag_id
FROM @SelectedTagIds selected
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.SeriesTag st
    WHERE st.series_id = @SeriesId
      AND st.tag_id = selected.tag_id
);

----------------------------------------------------------------------
-- Series contributors
-- These are intentionally easy to replace through the variables above.
----------------------------------------------------------------------

INSERT INTO manga.SeriesContributor
(
    series_id,
    user_id,
    start_date,
    notes
)
SELECT
    v.series_id,
    v.user_id,
    CONVERT(DATE, @Now),
    v.notes
FROM
(
    VALUES
    (
        @SeriesId,
        @MangakaUserId,
        N'Mock contributor: Mangaka owner for serialized series testing.'
    ),
    (
        @SeriesId,
        @AssistantUserId,
        N'Mock contributor: Assistant for page task testing.'
    ),
    (
        @SeriesId,
        @EditorUserId,
        N'Mock contributor: Tantou Editor for annotation/review testing.'
    )
) AS v(series_id, user_id, notes)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.SeriesContributor sc
    WHERE sc.series_id = v.series_id
      AND sc.user_id = v.user_id
      AND sc.start_date = CONVERT(DATE, @Now)
);

----------------------------------------------------------------------
-- Chapters
----------------------------------------------------------------------

INSERT INTO manga.Chapter
(
    chapter_id,
    series_id,
    chapter_number_label,
    chapter_title,
    status_code,
    planned_release_date,
    created_at_utc,
    created_by_user_id
)
SELECT
    v.chapter_id,
    v.series_id,
    v.chapter_number_label,
    v.chapter_title,
    v.status_code,
    v.planned_release_date,
    @Now,
    @MangakaUserId
FROM
(
    VALUES
    (
        @Chapter1Id,
        @SeriesId,
        N'1',
        N'Mock Chapter 1 - Under Review',
        N'UNDER_REVIEW',
        CONVERT(DATE, DATEADD(DAY, 7, @Now))
    ),
    (
        @Chapter2Id,
        @SeriesId,
        N'2',
        N'Mock Chapter 2 - Draft',
        N'DRAFT',
        CONVERT(DATE, DATEADD(DAY, 14, @Now))
    )
) AS v
(
    chapter_id,
    series_id,
    chapter_number_label,
    chapter_title,
    status_code,
    planned_release_date
)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.Chapter c
    WHERE c.chapter_id = v.chapter_id
       OR (
            c.series_id = v.series_id
            AND c.chapter_number_label = v.chapter_number_label
       )
);

----------------------------------------------------------------------
-- Chapter pages
----------------------------------------------------------------------

INSERT INTO manga.ChapterPage
(
    chapter_page_id,
    chapter_id,
    page_no,
    page_notes
)
SELECT
    v.chapter_page_id,
    v.chapter_id,
    v.page_no,
    v.page_notes
FROM
(
    VALUES
    (
        @Chapter1Page1Id,
        @Chapter1Id,
        1,
        N'Mock chapter 1 page 1.'
    ),
    (
        @Chapter1Page2Id,
        @Chapter1Id,
        2,
        N'Mock chapter 1 page 2 with assistant output version.'
    ),
    (
        @Chapter2Page1Id,
        @Chapter2Id,
        1,
        N'Mock chapter 2 page 1.'
    )
) AS v(chapter_page_id, chapter_id, page_no, page_notes)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.ChapterPage cp
    WHERE cp.chapter_page_id = v.chapter_page_id
);

----------------------------------------------------------------------
-- Chapter page versions
----------------------------------------------------------------------

INSERT INTO manga.ChapterPageVersion
(
    chapter_page_version_id,
    chapter_page_id,
    version_no,
    page_file_id,
    version_note,
    is_current_version
)
SELECT
    v.chapter_page_version_id,
    v.chapter_page_id,
    v.version_no,
    v.page_file_id,
    v.version_note,
    v.is_current_version
FROM
(
    VALUES
    (
        @Chapter1Page1VersionId,
        @Chapter1Page1Id,
        CONVERT(SMALLINT, 1),
        @Chapter1Page1FileId,
        N'Initial uploaded page version.',
        CONVERT(BIT, 1)
    ),
    (
        @Chapter1Page2Version1Id,
        @Chapter1Page2Id,
        CONVERT(SMALLINT, 1),
        @Chapter1Page2FileId,
        N'Initial uploaded page version before assistant output.',
        CONVERT(BIT, 0)
    ),
    (
        @Chapter1Page2Version2Id,
        @Chapter1Page2Id,
        CONVERT(SMALLINT, 2),
        @Chapter1Page2OutputFileId,
        N'Assistant output page version submitted for review.',
        CONVERT(BIT, 1)
    ),
    (
        @Chapter2Page1VersionId,
        @Chapter2Page1Id,
        CONVERT(SMALLINT, 1),
        @Chapter2Page1FileId,
        N'Initial uploaded page version.',
        CONVERT(BIT, 1)
    )
) AS v
(
    chapter_page_version_id,
    chapter_page_id,
    version_no,
    page_file_id,
    version_note,
    is_current_version
)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.ChapterPageVersion cpv
    WHERE cpv.chapter_page_version_id = v.chapter_page_version_id
       OR (
            cpv.chapter_page_id = v.chapter_page_id
            AND cpv.version_no = v.version_no
       )
);

----------------------------------------------------------------------
-- Page regions
----------------------------------------------------------------------

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
SELECT
    v.page_region_id,
    v.chapter_page_version_id,
    v.type_code,
    v.region_label,
    v.x,
    v.y,
    v.width,
    v.height,
    v.confidence_score,
    v.source_type,
    v.original_text,
    @Now,
    v.created_by_user_id
FROM
(
    VALUES
    (
        @Chapter1Page1PanelRegionId,
        @Chapter1Page1VersionId,
        N'PANEL',
        N'Chapter 1 Page 1 Main Panel',
        CONVERT(DECIMAL(10, 2), 25.00),
        CONVERT(DECIMAL(10, 2), 40.00),
        CONVERT(DECIMAL(10, 2), 720.00),
        CONVERT(DECIMAL(10, 2), 480.00),
        NULL,
        N'MANUAL',
        NULL,
        @MangakaUserId
    ),
    (
        @Chapter1Page1BubbleRegionId,
        @Chapter1Page1VersionId,
        N'SPEECH_BUBBLE',
        N'Chapter 1 Page 1 Dialogue Bubble',
        CONVERT(DECIMAL(10, 2), 110.00),
        CONVERT(DECIMAL(10, 2), 95.00),
        CONVERT(DECIMAL(10, 2), 260.00),
        CONVERT(DECIMAL(10, 2), 120.00),
        NULL,
        N'MANUAL',
        N'I will finish this chapter today!',
        @EditorUserId
    ),
    (
        @Chapter1Page2PanelRegionId,
        @Chapter1Page2Version1Id,
        N'PANEL',
        N'Chapter 1 Page 2 Background Panel',
        CONVERT(DECIMAL(10, 2), 30.00),
        CONVERT(DECIMAL(10, 2), 55.00),
        CONVERT(DECIMAL(10, 2), 700.00),
        CONVERT(DECIMAL(10, 2), 510.00),
        NULL,
        N'MANUAL',
        NULL,
        @MangakaUserId
    ),
    (
        @Chapter2Page1PanelRegionId,
        @Chapter2Page1VersionId,
        N'PANEL',
        N'Chapter 2 Page 1 Opening Panel',
        CONVERT(DECIMAL(10, 2), 45.00),
        CONVERT(DECIMAL(10, 2), 60.00),
        CONVERT(DECIMAL(10, 2), 680.00),
        CONVERT(DECIMAL(10, 2), 500.00),
        NULL,
        N'MANUAL',
        NULL,
        @MangakaUserId
    )
) AS v
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
    created_by_user_id
)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.PageRegion pr
    WHERE pr.page_region_id = v.page_region_id
);

----------------------------------------------------------------------
-- Page tasks
----------------------------------------------------------------------

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
    created_by_user_id
)
SELECT
    v.chapter_page_task_id,
    v.assigned_to_user_id,
    v.type_code,
    v.status_code,
    v.task_title,
    v.task_description,
    v.priority_level,
    v.due_at_utc,
    v.compensation_amount,
    v.completed_page_version_id,
    @Now,
    v.created_by_user_id
FROM
(
    VALUES
    (
        @Task1Id,
        @AssistantUserId,
        N'CLEANUP',
        N'ASSIGNED',
        N'Clean dialogue bubble lines',
        N'Clean the bubble border and remove stray sketch marks on chapter 1 page 1.',
        CONVERT(TINYINT, 2),
        DATEADD(DAY, 3, @Now),
        CONVERT(DECIMAL(12, 2), 150000.00),
        CONVERT(UNIQUEIDENTIFIER, NULL),
        @MangakaUserId
    ),
    (
        @Task2Id,
        @AssistantUserId,
        N'BACKGROUND',
        N'UNDER_REVIEW',
        N'Rework background shading',
        N'Assistant submitted a revised background version for review on chapter 1 page 2.',
        CONVERT(TINYINT, 3),
        DATEADD(DAY, 5, @Now),
        CONVERT(DECIMAL(12, 2), 250000.00),
        @Chapter1Page2Version2Id,
        @MangakaUserId
    )
) AS v
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
    created_by_user_id
)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.ChapterPageTask t
    WHERE t.chapter_page_task_id = v.chapter_page_task_id
);

----------------------------------------------------------------------
-- Task-region links
----------------------------------------------------------------------

INSERT INTO manga.ChapterPageTaskRegion
(
    chapter_page_task_id,
    page_region_id
)
SELECT
    v.chapter_page_task_id,
    v.page_region_id
FROM
(
    VALUES
    (
        @Task1Id,
        @Chapter1Page1BubbleRegionId
    ),
    (
        @Task2Id,
        @Chapter1Page2PanelRegionId
    )
) AS v(chapter_page_task_id, page_region_id)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.ChapterPageTaskRegion tr
    WHERE tr.chapter_page_task_id = v.chapter_page_task_id
      AND tr.page_region_id = v.page_region_id
);

----------------------------------------------------------------------
-- Annotations
----------------------------------------------------------------------

INSERT INTO manga.ChapterPageAnnotation
(
    chapter_page_annotation_id,
    issue_type_code,
    annotated_by_user_id,
    annotation_text,
    resolved_at_utc,
    resolved_by_user_id,
    created_at_utc
)
SELECT
    v.chapter_page_annotation_id,
    v.issue_type_code,
    v.annotated_by_user_id,
    v.annotation_text,
    v.resolved_at_utc,
    v.resolved_by_user_id,
    @Now
FROM
(
    VALUES
    (
        @Annotation1Id,
        N'DIALOGUE_ERROR',
        @EditorUserId,
        N'The dialogue text does not match the intended emotional tone. Please revise the wording.',
        CONVERT(DATETIME2(0), NULL),
        CONVERT(UNIQUEIDENTIFIER, NULL)
    ),
    (
        @Annotation2Id,
        N'SHADING_ERROR',
        @EditorUserId,
        N'The background shading is too dark and hides the character silhouette.',
        DATEADD(HOUR, 2, @Now),
        @EditorUserId
    )
) AS v
(
    chapter_page_annotation_id,
    issue_type_code,
    annotated_by_user_id,
    annotation_text,
    resolved_at_utc,
    resolved_by_user_id
)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.ChapterPageAnnotation a
    WHERE a.chapter_page_annotation_id = v.chapter_page_annotation_id
);

----------------------------------------------------------------------
-- Annotation-region links
----------------------------------------------------------------------

INSERT INTO manga.ChapterPageAnnotationRegion
(
    chapter_page_annotation_id,
    page_region_id
)
SELECT
    v.chapter_page_annotation_id,
    v.page_region_id
FROM
(
    VALUES
    (
        @Annotation1Id,
        @Chapter1Page1BubbleRegionId
    ),
    (
        @Annotation2Id,
        @Chapter1Page2PanelRegionId
    )
) AS v(chapter_page_annotation_id, page_region_id)
WHERE NOT EXISTS
(
    SELECT 1
    FROM manga.ChapterPageAnnotationRegion ar
    WHERE ar.chapter_page_annotation_id = v.chapter_page_annotation_id
      AND ar.page_region_id = v.page_region_id
);

----------------------------------------------------------------------
-- Verification output
----------------------------------------------------------------------

SELECT
    s.series_id,
    s.title,
    s.slug,
    s.status_code,
    s.publication_frequency_code,
    Genres =
    (
        SELECT STRING_AGG(g.genre_name, N', ')
        FROM manga.SeriesGenre sg
        INNER JOIN manga.Genre g
            ON g.genre_id = sg.genre_id
        WHERE sg.series_id = s.series_id
    ),
    Tags =
    (
        SELECT STRING_AGG(t.tag_name, N', ')
        FROM manga.SeriesTag st
        INNER JOIN manga.Tag t
            ON t.tag_id = st.tag_id
        WHERE st.series_id = s.series_id
    )
FROM manga.Series s
WHERE s.series_id = @SeriesId;

SELECT
    c.chapter_id,
    c.chapter_number_label,
    c.chapter_title,
    c.status_code
FROM manga.Chapter c
WHERE c.series_id = @SeriesId
ORDER BY c.chapter_number_label;

SELECT
    cp.chapter_page_id,
    c.chapter_number_label,
    cp.page_no,
    cpv.chapter_page_version_id,
    cpv.version_no,
    cpv.is_current_version
FROM manga.Chapter c
INNER JOIN manga.ChapterPage cp
    ON cp.chapter_id = c.chapter_id
INNER JOIN manga.ChapterPageVersion cpv
    ON cpv.chapter_page_id = cp.chapter_page_id
WHERE c.series_id = @SeriesId
ORDER BY c.chapter_number_label, cp.page_no, cpv.version_no;

SELECT
    t.chapter_page_task_id,
    t.task_title,
    t.type_code,
    t.status_code,
    t.assigned_to_user_id,
    t.completed_page_version_id
FROM manga.ChapterPageTask t
WHERE t.chapter_page_task_id IN (@Task1Id, @Task2Id);

SELECT
    a.chapter_page_annotation_id,
    a.issue_type_code,
    a.annotation_text,
    a.resolved_at_utc,
    a.resolved_by_user_id
FROM manga.ChapterPageAnnotation a
WHERE a.chapter_page_annotation_id IN (@Annotation1Id, @Annotation2Id);
GO