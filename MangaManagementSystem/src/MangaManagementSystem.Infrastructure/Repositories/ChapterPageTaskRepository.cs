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
    public class ChapterPageTaskRepository : GenericRepository<ChapterPageTask>, IChapterPageTaskRepository
    {
        private readonly ApplicationDbContext _context;
        public ChapterPageTaskRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Guid> CreateChapterPageTaskAsync(
            Guid actorUserId,
            Guid assignedToUserId,
            string typeCode,
            string taskTitle,
            string taskDescription,
            byte priorityLevel,
            DateTime dueAtUtc,
            decimal? compensationAmount,
            IReadOnlyList<Guid> pageRegionIds)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_ChapterPageTask_Create";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@actor_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@assigned_to_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = assignedToUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@type_code", System.Data.SqlDbType.NVarChar, 50) { Value = typeCode });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@task_title", System.Data.SqlDbType.NVarChar, 200) { Value = taskTitle });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@task_description", System.Data.SqlDbType.NVarChar, -1) { Value = taskDescription });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@priority_level", System.Data.SqlDbType.TinyInt) { Value = priorityLevel });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@due_at_utc", System.Data.SqlDbType.DateTime2) { Value = dueAtUtc });
            
            var compParam = new Microsoft.Data.SqlClient.SqlParameter("@compensation_amount", System.Data.SqlDbType.Decimal) { Precision = 12, Scale = 2 };
            if (compensationAmount.HasValue) compParam.Value = compensationAmount.Value; else compParam.Value = 0m;
            cmd.Parameters.Add(compParam);

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@page_region_ids_json", System.Data.SqlDbType.NVarChar, -1) { Value = System.Text.Json.JsonSerializer.Serialize(pageRegionIds) });
            
            var newIdParam = new Microsoft.Data.SqlClient.SqlParameter("@new_chapter_page_task_id", System.Data.SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(newIdParam);

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();

            return (Guid)newIdParam.Value;
        }

        public async Task<ChapterPageTask?> GetByIdWithRegionsAsync(Guid id)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.PageRegions)
                .FirstOrDefaultAsync(t => t.ChapterPageTaskId == id);
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetByAssignedUserIdWithRegionsAsync(Guid assignedToUserId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.PageRegions)
                .Where(t => t.AssignedToUserId == assignedToUserId)
                .OrderBy(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetByCreatorUserIdWithSeriesAsync(Guid creatorUserId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.ChapterPage)
                            .ThenInclude(p => p.Chapter)
                .Include(t => t.AssignedToUser)
                .Where(t => t.CreatedByUserId == creatorUserId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetByAssignedUserIdWithFullContextAsync(Guid assignedToUserId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CompletedPageVersion)
                    .ThenInclude(v => v!.PageFile)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.ChapterPage)
                            .ThenInclude(p => p.Chapter)
                                .ThenInclude(c => c.Series)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.PageFile)
                .Where(t => t.AssignedToUserId == assignedToUserId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<ChapterPageTask?> GetByIdWithFullContextAsync(Guid taskId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.CompletedPageVersion)
                    .ThenInclude(v => v!.PageFile)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.ChapterPage)
                            .ThenInclude(p => p.Chapter)
                                .ThenInclude(c => c.Series)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.PageFile)
                .FirstOrDefaultAsync(t => t.ChapterPageTaskId == taskId);
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetByChapterPageIdWithRegionsAsync(Guid chapterPageId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.PageRegions)
                .Where(t => t.PageRegions.Any(r => r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPageId == chapterPageId))
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        // --- Mangaka task lifecycle SPs ---

        public async Task CancelTaskAsync(Guid actorUserId, Guid taskId, string reason)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_ChapterPageTask_Cancel";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@actor_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@chapter_page_task_id", System.Data.SqlDbType.UniqueIdentifier) { Value = taskId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@reason", System.Data.SqlDbType.NVarChar, 500) { Value = reason });

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarkTaskCompletedAsync(Guid actorUserId, Guid taskId, string? completionNote)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_ChapterPageTask_MarkCompleted";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@actor_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@chapter_page_task_id", System.Data.SqlDbType.UniqueIdentifier) { Value = taskId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@completion_note", System.Data.SqlDbType.NVarChar, -1)
            {
                Value = string.IsNullOrWhiteSpace(completionNote) ? (object)DBNull.Value : completionNote
            });

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ReturnTaskForReworkAsync(Guid actorUserId, Guid taskId, string updatedTaskDescription)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_ChapterPageTask_ReturnForRework";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@actor_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@chapter_page_task_id", System.Data.SqlDbType.UniqueIdentifier) { Value = taskId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@updated_task_description", System.Data.SqlDbType.NVarChar, -1) { Value = updatedTaskDescription });

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetTasksForReviewByCreatorAsync(Guid creatorUserId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.CompletedPageVersion)
                    .ThenInclude(v => v!.PageFile)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.ChapterPage)
                            .ThenInclude(p => p.Chapter)
                                .ThenInclude(c => c.Series)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.PageFile)
                .Where(t => t.CreatedByUserId == creatorUserId)
                .OrderByDescending(t => t.UpdatedAtUtc ?? t.CreatedAtUtc)
                .ToListAsync();
        }
    }
}
