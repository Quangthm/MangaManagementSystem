using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterRepository : GenericRepository<Chapter>, IChapterRepository
    {
        public ChapterRepository(ApplicationDbContext context) : base(context) { }

        public async Task DeleteWithDependenciesAsync(Guid chapterId)
        {
            // The designed stored procedure manga.usp_Chapter_Delete does not exist in this
            // database ("Could not find stored procedure"), so the cascade is performed here.
            // FKs have no ON DELETE CASCADE, so dependants are removed in dependency order
            // inside one transaction. Set-based deletes keep this fast even when a page has a
            // very large number of regions. No schema/objects are created — data only.
            const string sql = @"
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRY
    BEGIN TRAN;

    SELECT v.chapter_page_version_id AS id
    INTO #vers
    FROM manga.ChapterPageVersion v
    INNER JOIN manga.ChapterPage p ON p.chapter_page_id = v.chapter_page_id
    WHERE p.chapter_id = {0};

    SELECT r.page_region_id AS id
    INTO #regs
    FROM manga.PageRegion r
    WHERE r.chapter_page_version_id IN (SELECT id FROM #vers);

    SELECT DISTINCT tr.chapter_page_task_id AS id
    INTO #tasks
    FROM manga.ChapterPageTaskRegion tr
    WHERE tr.page_region_id IN (SELECT id FROM #regs);

    INSERT INTO #tasks (id)
    SELECT t.chapter_page_task_id
    FROM manga.ChapterPageTask t
    WHERE t.completed_page_version_id IN (SELECT id FROM #vers)
      AND t.chapter_page_task_id NOT IN (SELECT id FROM #tasks);

    SELECT DISTINCT ar.chapter_page_annotation_id AS id
    INTO #anns
    FROM manga.ChapterPageAnnotationRegion ar
    WHERE ar.page_region_id IN (SELECT id FROM #regs);

    DELETE FROM manga.ChapterPageTaskRegion WHERE chapter_page_task_id IN (SELECT id FROM #tasks);
    DELETE FROM manga.ChapterPageTaskRegion WHERE page_region_id IN (SELECT id FROM #regs);
    DELETE FROM manga.ChapterPageAnnotationRegion WHERE chapter_page_annotation_id IN (SELECT id FROM #anns);
    DELETE FROM manga.ChapterPageAnnotationRegion WHERE page_region_id IN (SELECT id FROM #regs);
    DELETE FROM manga.ChapterPageTask WHERE chapter_page_task_id IN (SELECT id FROM #tasks);
    DELETE FROM manga.ChapterPageAnnotation WHERE chapter_page_annotation_id IN (SELECT id FROM #anns);
    DELETE FROM manga.PageRegion WHERE chapter_page_version_id IN (SELECT id FROM #vers);
    DELETE FROM manga.ChapterPageVersion WHERE chapter_page_id IN (SELECT chapter_page_id FROM manga.ChapterPage WHERE chapter_id = {0});
    DELETE FROM manga.ChapterPage WHERE chapter_id = {0};
    DELETE FROM manga.ChapterEditorialReview WHERE chapter_id = {0};
    DELETE FROM manga.ChapterReaderVoteSnapshot WHERE chapter_id = {0};
    DELETE FROM manga.Chapter WHERE chapter_id = {0};

    DROP TABLE #vers;
    DROP TABLE #regs;
    DROP TABLE #tasks;
    DROP TABLE #anns;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;";

            await _context.Database.ExecuteSqlRawAsync(sql, chapterId);
        }
    }
}
