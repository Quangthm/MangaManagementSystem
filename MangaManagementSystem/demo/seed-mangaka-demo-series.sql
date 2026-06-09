/* =============================================================================
   DEV / DEMO ONLY  --  NOT FOR PRODUCTION
   -----------------------------------------------------------------------------
   Seeds 1-2 demo manga series so the Mangaka dashboard (/mangaka) and the
   Mangaka series list (/mangaka/series) have realistic data to display during
   local demos. Both pages read via ISeriesService.GetAllSeriesAsync(), which
   returns ALL series with no owner filter, so inserting Series rows alone is
   enough for the UI to show data.

   Safe to run multiple times (idempotent): each insert is guarded by the unique
   `slug`. Existing real series and user data are never modified or deleted.

   Verified against MangaManagementSystem_Schema.sql:
     - manga.Series required NOT NULL: series_code (UNIQUE), title, slug (UNIQUE),
       synopsis, genre, status_code, content_language_code, created_at_utc.
     - status_code CHECK: PROPOSAL_DRAFT, UNDER_EDITORIAL_REVIEW,
       UNDER_BOARD_REVIEW, SERIALIZED, HIATUS, CANCELLED, COMPLETED.
     - content_language_code CHECK: ja, en, vi.
     - publication_frequency_code CHECK: WEEKLY, MONTHLY, IRREGULAR or NULL.
     - updated_at_utc / updated_by_user_id are a paired CHECK: leave BOTH NULL.
     - cover_file_id is nullable: left NULL (no Cloudinary file required).

   How to run manually (do NOT run against production):
     sqlcmd -S <server> -d MangaManagementDB -i MangaManagementSystem/demo/seed-mangaka-demo-series.sql
   or open this file in SSMS connected to the local MangaManagementDB and execute.

   NOTE on SeriesProposal: a proposal row is intentionally NOT seeded here.
   manga.SeriesProposal.proposal_file_id is NOT NULL (FK to manga.FileResource),
   and seeding a proposal would require a real uploaded file. Deferred on purpose.
   ============================================================================= */

USE MangaManagementDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ----------------------------------------------------------------------------
   1) Demo series (idempotent by slug + matching unique series_code)
   ---------------------------------------------------------------------------- */

-- Crimson Moon Academy
IF NOT EXISTS (SELECT 1 FROM manga.Series WHERE slug = N'crimson-moon-academy')
BEGIN
    INSERT INTO manga.Series
        (series_code, title, slug, synopsis, genre,
         status_code, content_language_code, publication_frequency_code)
    VALUES
        (N'DEMO-CRIMSON-MOON',
         N'Crimson Moon Academy',
         N'crimson-moon-academy',
         N'At an elite academy hidden beneath a perpetual crimson moon, a first-year student discovers she can see the spirits bound to the school grounds and is pulled into a centuries-old pact that the faculty would rather keep buried.',
         N'Supernatural / School Fantasy',
         N'PROPOSAL_DRAFT',
         N'ja',
         NULL); -- frequency not decided yet for a draft proposal
END;

-- Neon Samurai Path
IF NOT EXISTS (SELECT 1 FROM manga.Series WHERE slug = N'neon-samurai-path')
BEGIN
    INSERT INTO manga.Series
        (series_code, title, slug, synopsis, genre,
         status_code, content_language_code, publication_frequency_code)
    VALUES
        (N'DEMO-NEON-SAMURAI',
         N'Neon Samurai Path',
         N'neon-samurai-path',
         N'In a rain-soaked megacity where corporate clans wage proxy wars through augmented mercenaries, a disgraced swordsman trades his blade for a smart-katana and walks a narrow path between revenge and the code he swore to abandon.',
         N'Cyberpunk / Action',
         N'SERIALIZED',
         N'en',
         N'MONTHLY');
END;

/* ----------------------------------------------------------------------------
   2) Optional SeriesContributor (Mangaka owner) -- guarded, never creates users
   ---------------------------------------------------------------------------- */

DECLARE @mangakaUserId INT;

SELECT TOP (1) @mangakaUserId = u.user_id
FROM auth.Users AS u
JOIN auth.Roles AS r ON r.role_id = u.role_id
WHERE r.role_name = N'Mangaka'
  AND u.status_code = N'ACTIVE'
ORDER BY u.user_id;

IF @mangakaUserId IS NULL
BEGIN
    PRINT 'No active Mangaka user found - skipping SeriesContributor seed. (Series rows were still inserted.)';
END
ELSE
BEGIN
    DECLARE @crimsonId BIGINT = (SELECT series_id FROM manga.Series WHERE slug = N'crimson-moon-academy');
    DECLARE @neonId    BIGINT = (SELECT series_id FROM manga.Series WHERE slug = N'neon-samurai-path');

    IF @crimsonId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM manga.SeriesContributor
                       WHERE series_id = @crimsonId AND user_id = @mangakaUserId AND end_date IS NULL)
    BEGIN
        INSERT INTO manga.SeriesContributor (series_id, user_id, notes)
        VALUES (@crimsonId, @mangakaUserId, N'Demo owner (Mangaka) seed.');
    END;

    IF @neonId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM manga.SeriesContributor
                       WHERE series_id = @neonId AND user_id = @mangakaUserId AND end_date IS NULL)
    BEGIN
        INSERT INTO manga.SeriesContributor (series_id, user_id, notes)
        VALUES (@neonId, @mangakaUserId, N'Demo owner (Mangaka) seed.');
    END;

    PRINT 'Linked demo series to Mangaka user_id = ' + CAST(@mangakaUserId AS NVARCHAR(10)) + '.';
END;

COMMIT TRANSACTION;
GO

/* ----------------------------------------------------------------------------
   3) Verification -- show the demo rows
   ---------------------------------------------------------------------------- */
SELECT series_id, series_code, title, slug, status_code,
       content_language_code, publication_frequency_code, created_at_utc
FROM manga.Series
WHERE slug IN (N'crimson-moon-academy', N'neon-samurai-path')
ORDER BY series_id;
GO
