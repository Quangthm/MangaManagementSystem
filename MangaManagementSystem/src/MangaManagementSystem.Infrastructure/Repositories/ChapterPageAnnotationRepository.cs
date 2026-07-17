using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterPageAnnotationRepository : GenericRepository<ChapterPageAnnotation>, IChapterPageAnnotationRepository
    {
        public ChapterPageAnnotationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Guid> CreateChapterPageAnnotationAsync(
            Guid actorUserId,
            IReadOnlyList<Guid> pageRegionIds,
            string issueTypeCode,
            string annotationText)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_ChapterPageAnnotation_Create";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@actor_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@issue_type_code", System.Data.SqlDbType.NVarChar, 50) { Value = issueTypeCode });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@annotation_text", System.Data.SqlDbType.NVarChar, 1000) { Value = annotationText });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@page_region_ids_json", System.Data.SqlDbType.NVarChar, -1) { Value = System.Text.Json.JsonSerializer.Serialize(pageRegionIds) });

            var newIdParam = new Microsoft.Data.SqlClient.SqlParameter("@new_chapter_page_annotation_id", System.Data.SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(newIdParam);

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();

            return (Guid)newIdParam.Value;
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

        public async Task<IReadOnlyList<ChapterPageAnnotation>> GetByPageRegionIdsAsync(IReadOnlyList<Guid> pageRegionIds)
        {
            if (pageRegionIds == null || pageRegionIds.Count == 0)
                return Array.Empty<ChapterPageAnnotation>();

            return await _context.ChapterPageAnnotations
                .Include(a => a.PageRegions)
                .Include(a => a.AnnotatedByUser)
                .Where(a => a.PageRegions.Any(r => pageRegionIds.Contains(r.PageRegionId)))
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<bool> ResolveAnnotationAsync(
            Guid actorUserId,
            Guid annotationId,
            string? resolutionNote = null)
        {
            // BR-ANN-021: an annotation created by a Tantou Editor may be resolved ONLY by a Tantou Editor;
            // a Mangaka must not resolve it. (Mangaka-created annotations stay resolvable by either role,
            // BR-ANN-020.) The resolve SP only checks active-contributor, so enforce the creator/actor role
            // matrix here in the repository. Role is derived from the creator's current role (BR-ANN-025).
            var creatorRoleName = await _context.ChapterPageAnnotations
                .Where(a => a.ChapterPageAnnotationId == annotationId)
                .Select(a => a.AnnotatedByUser != null && a.AnnotatedByUser.Role != null ? a.AnnotatedByUser.Role.RoleName : null)
                .FirstOrDefaultAsync();

            if (string.Equals(creatorRoleName, "Tantou Editor", StringComparison.Ordinal))
            {
                var actorRoleName = await _context.Users
                    .Where(u => u.UserId == actorUserId)
                    .Select(u => u.Role != null ? u.Role.RoleName : null)
                    .FirstOrDefaultAsync();

                if (!string.Equals(actorRoleName, "Tantou Editor", StringComparison.Ordinal))
                    throw new InvalidOperationException("Only a Tantou Editor can resolve an annotation created by a Tantou Editor.");
            }

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
                .Include(a => a.AnnotatedByUser)
                    .ThenInclude(u => u!.Role)
                .Where(a => a.PageRegions.Any(r => r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPageId == chapterPageId))
                .ToListAsync();
        }
    }
}
