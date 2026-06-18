using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterPageAnnotationRepository : GenericRepository<ChapterPageAnnotation>, IChapterPageAnnotationRepository
    {
        private readonly ApplicationDbContext _context;
        public ChapterPageAnnotationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Guid> CreateChapterPageAnnotationAsync(
            Guid actorUserId,
            IReadOnlyList<Guid> pageRegionIds,
            string issueTypeCode,
            string annotationText)
        {
            var annotation = new ChapterPageAnnotation
            {
                ChapterPageAnnotationId = Guid.NewGuid(),
                AnnotatedByUserId = actorUserId,
                IssueTypeCode = issueTypeCode,
                AnnotationText = annotationText,
                CreatedAtUtc = DateTime.UtcNow
            };

            if (pageRegionIds != null && pageRegionIds.Any())
            {
                var regions = await _context.PageRegions
                    .Where(r => pageRegionIds.Contains(r.PageRegionId))
                    .ToListAsync();
                    
                foreach (var region in regions)
                {
                    annotation.PageRegions.Add(region);
                }
            }

            await _context.ChapterPageAnnotations.AddAsync(annotation);
            await _context.SaveChangesAsync();

            return annotation.ChapterPageAnnotationId;
        }

        public async Task<ChapterPageAnnotation?> GetByIdWithRegionsAsync(Guid id)
        {
            return await _context.ChapterPageAnnotations
                .Include(a => a.PageRegions)
                .FirstOrDefaultAsync(a => a.ChapterPageAnnotationId == id);
        }

        public async Task<IReadOnlyList<ChapterPageAnnotation>> GetByPageRegionIdAsync(Guid pageRegionId)
        {
            return await _context.ChapterPageAnnotations
                .Include(a => a.PageRegions)
                .Where(a => a.PageRegions.Any(r => r.PageRegionId == pageRegionId))
                .ToListAsync();
        }

        public async Task<bool> ResolveAnnotationAsync(
            Guid actorUserId,
            Guid annotationId,
            string? resolutionNote = null)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_ChapterPageAnnotation_Resolve";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@actor_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@chapter_page_annotation_id", System.Data.SqlDbType.UniqueIdentifier) { Value = annotationId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@resolution_note", System.Data.SqlDbType.NVarChar, 500) { 
                Value = string.IsNullOrWhiteSpace(resolutionNote) ? System.DBNull.Value : (object)resolutionNote 
            });

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<IReadOnlyList<ChapterPageAnnotation>> GetByChapterPageIdWithRegionsAsync(Guid chapterPageId)
        {
            return await _context.ChapterPageAnnotations
                .Include(a => a.PageRegions)
                .Where(a => a.PageRegions.Any(r => r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPageId == chapterPageId))
                .ToListAsync();
        }
    }
}
