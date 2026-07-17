-- ============================================================================
-- completed_test_series.sql
-- Test fixture: a series already in COMPLETED status, owned by TestMangaka1,
-- for verifying the "Series COMPLETED locks content" workspace behavior
-- (UI guard + server guard in MangakaChapterRepository.EnsureSeriesAcceptsNewChapterAsync).
--
-- Idempotent: safe to run multiple times (guarded by WHERE NOT EXISTS).
-- To remove it again, see the CLEANUP block at the bottom (commented out).
--
-- Run against MangaManagementDB (sqlcmd -I ... or SSMS).
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

-- Resolve the Mangaka owner (TestMangaka1). Adjust the username if your seed differs.
DECLARE @MangakaUserId UNIQUEIDENTIFIER = NULL;
SELECT @MangakaUserId = u.user_id
FROM auth.Users u
WHERE u.username = N'TestMangaka1';

IF @MangakaUserId IS NULL
    THROW 59911, 'TestMangaka1 not found. Set @MangakaUserId to a real auth.Users.user_id.', 1;

-- Fixed GUIDs (distinct from mock_serialized_series.sql's 1000.../2000... ranges).
DECLARE @SeriesId   UNIQUEIDENTIFIER = '1C000000-0000-0000-0000-000000000001';
DECLARE @Chapter1Id UNIQUEIDENTIFIER = '2C000000-0000-0000-0000-000000000001';
DECLARE @Chapter2Id UNIQUEIDENTIFIER = '2C000000-0000-0000-0000-000000000002';

----------------------------------------------------------------------
-- 1) The COMPLETED series (no cover; cover_file_id is nullable)
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
    N'Completed Test Series',
    N'completed-test-series',
    N'A finished series used to test that a COMPLETED series locks chapter content and blocks new chapters.',
    NULL,
    N'COMPLETED',
    N'ja',
    @Now,
    N'WEEKLY'
WHERE NOT EXISTS
(
    SELECT 1 FROM manga.Series s
    WHERE s.series_id = @SeriesId OR s.slug = N'completed-test-series'
);

----------------------------------------------------------------------
-- 2) Mangaka contributor (active: end_date NULL) so the workspace opens
----------------------------------------------------------------------
INSERT INTO manga.SeriesContributor
(
    series_id,
    user_id,
    start_date,
    notes
)
SELECT
    @SeriesId,
    @MangakaUserId,
    CONVERT(DATE, @Now),
    N'Mangaka owner for COMPLETED-series lock testing.'
WHERE NOT EXISTS
(
    SELECT 1 FROM manga.SeriesContributor sc
    WHERE sc.series_id = @SeriesId AND sc.user_id = @MangakaUserId
);

----------------------------------------------------------------------
-- 3) A couple of non-editable chapters so there is content to view.
--    Ch.1 RELEASED (released_at_utc required); Ch.2 APPROVED.
----------------------------------------------------------------------
INSERT INTO manga.Chapter
(
    chapter_id,
    series_id,
    chapter_number_label,
    chapter_title,
    status_code,
    planned_release_date,
    released_at_utc,
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
    v.released_at_utc,
    @Now,
    @MangakaUserId
FROM
(
    VALUES
    (
        @Chapter1Id, @SeriesId, N'1', N'Finale Part 1',
        N'RELEASED', CONVERT(DATE, DATEADD(DAY, -7, @Now)), DATEADD(DAY, -7, @Now)
    ),
    (
        @Chapter2Id, @SeriesId, N'2', N'Finale Part 2',
        N'APPROVED', NULL, NULL
    )
) AS v(chapter_id, series_id, chapter_number_label, chapter_title, status_code, planned_release_date, released_at_utc)
WHERE NOT EXISTS
(
    SELECT 1 FROM manga.Chapter c
    WHERE c.chapter_id = v.chapter_id
       OR (c.series_id = v.series_id AND c.chapter_number_label = v.chapter_number_label)
);

PRINT 'completed_test_series.sql applied. Open /series/completed-test-series as TestMangaka1.';

----------------------------------------------------------------------
-- CLEANUP (uncomment to remove this fixture):
-- DELETE FROM manga.Chapter          WHERE series_id = '1C000000-0000-0000-0000-000000000001';
-- DELETE FROM manga.SeriesContributor WHERE series_id = '1C000000-0000-0000-0000-000000000001';
-- DELETE FROM manga.Series            WHERE series_id = '1C000000-0000-0000-0000-000000000001';
----------------------------------------------------------------------
