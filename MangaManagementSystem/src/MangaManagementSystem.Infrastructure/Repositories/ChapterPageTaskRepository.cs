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
            var task = new ChapterPageTask
            {
                ChapterPageTaskId = Guid.NewGuid(),
                AssignedToUserId = assignedToUserId,
                TypeCode = typeCode,
                TaskTitle = taskTitle,
                TaskDescription = taskDescription,
                PriorityLevel = priorityLevel,
                DueAtUtc = dueAtUtc,
                CompensationAmount = compensationAmount ?? 0m,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = DateTime.UtcNow,
                StatusCode = "ASSIGNED"
            };

            if (pageRegionIds != null && pageRegionIds.Any())
            {
                // Create unique IDs to attach
                var uniqueIds = pageRegionIds.Distinct().ToList();
                foreach (var regionId in uniqueIds)
                {
                    // Check if already tracked to avoid InvalidOperationException
                    var trackedRegion = _context.PageRegions.Local.FirstOrDefault(r => r.PageRegionId == regionId);
                    if (trackedRegion == null)
                    {
                        trackedRegion = new PageRegion { PageRegionId = regionId };
                        _context.PageRegions.Attach(trackedRegion);
                    }
                    task.PageRegions.Add(trackedRegion);
                }
            }

            await _context.ChapterPageTasks.AddAsync(task);
            await _context.SaveChangesAsync();

            return task.ChapterPageTaskId;
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
