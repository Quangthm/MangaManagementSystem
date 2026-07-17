using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Domain.Policies;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterPageTaskRepository : GenericRepository<ChapterPageTask>, IChapterPageTaskRepository
    {
        private const string ActiveUserStatusCode = "ACTIVE";
        private const string MangakaRoleName = "Mangaka";
        private const string AssistantRoleName = "Assistant";

        private readonly ILogger<ChapterPageTaskRepository> _logger;

        public ChapterPageTaskRepository(
            ApplicationDbContext context,
            ILogger<ChapterPageTaskRepository> logger) : base(context)
        {
            _logger = logger;
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
            var normalizedRegionIds = (pageRegionIds ?? Array.Empty<Guid>())
                .Distinct()
                .ToArray();

            if (normalizedRegionIds.Length == 0)
                throw new InvalidOperationException("At least one page region is required.");

            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.RepeatableRead);

            try
            {
                var regions = await _context.PageRegions
                    .Where(region => normalizedRegionIds.Contains(region.PageRegionId))
                    .Include(region => region.ChapterPageVersion)
                        .ThenInclude(version => version!.ChapterPage)
                            .ThenInclude(page => page!.Chapter)
                                .ThenInclude(chapter => chapter!.Series)
                    .ToListAsync();

                if (regions.Count != normalizedRegionIds.Length)
                {
                    throw new InvalidOperationException(
                        "All selected page regions must exist.");
                }

                if (regions.Select(region => region.ChapterPageVersionId).Distinct().Count() != 1)
                {
                    throw new InvalidOperationException(
                        "All selected page regions must belong to the same chapter page version.");
                }

                var pageVersion = regions[0].ChapterPageVersion;
                var page = pageVersion?.ChapterPage;
                var chapter = page?.Chapter;
                var series = chapter?.Series;

                if (pageVersion == null || page == null || chapter == null || series == null)
                {
                    throw new InvalidOperationException(
                        "The selected page-region context is no longer valid.");
                }

                var actor = await _context.Users
                    .Include(user => user.Role)
                    .FirstOrDefaultAsync(user => user.UserId == actorUserId);

                if (actor == null
                    || !string.Equals(actor.StatusCode, ActiveUserStatusCode, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(actor.Role?.RoleName, MangakaRoleName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "An active Mangaka account is required to create page tasks.");
                }

                var assignee = await _context.Users
                    .Include(user => user.Role)
                    .FirstOrDefaultAsync(user => user.UserId == assignedToUserId);

                if (assignee == null
                    || !string.Equals(assignee.StatusCode, ActiveUserStatusCode, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(assignee.Role?.RoleName, AssistantRoleName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The assigned user must be an active Assistant.");
                }

                var contributorUserIds = await _context.SeriesContributors
                    .Where(contributor =>
                        contributor.SeriesId == series.SeriesId
                        && contributor.EndDate == null
                        && (contributor.UserId == actorUserId
                            || contributor.UserId == assignedToUserId))
                    .Select(contributor => contributor.UserId)
                    .Distinct()
                    .ToListAsync();

                if (!contributorUserIds.Contains(actorUserId))
                {
                    throw new InvalidOperationException(
                        "You must be an active Mangaka contributor of this series to create page tasks.");
                }

                if (!contributorUserIds.Contains(assignedToUserId))
                {
                    throw new InvalidOperationException(
                        "The assigned Assistant must be an active contributor of this series.");
                }

                if (!SeriesProductionPolicy.AllowsNormalProduction(series.StatusCode)
                    || !ChapterPageTaskCreationPolicy.CanCreateTask(chapter.StatusCode))
                {
                    throw new InvalidOperationException(
                        "New tasks can only be created for DRAFT or REVISION_REQUESTED chapters in a SERIALIZED or HIATUS series.");
                }

                var createdAtUtc = DateTime.UtcNow;
                var task = new ChapterPageTask
                {
                    AssignedToUserId = assignedToUserId,
                    TypeCode = typeCode,
                    StatusCode = "ASSIGNED",
                    TaskTitle = taskTitle,
                    TaskDescription = taskDescription,
                    PriorityLevel = priorityLevel,
                    DueAtUtc = dueAtUtc,
                    CompensationAmount = compensationAmount ?? 0m,
                    CompletedPageVersionId = null,
                    CreatedAtUtc = createdAtUtc,
                    CreatedByUserId = actorUserId,
                    UpdatedAtUtc = null,
                    PageRegions = regions
                };

                _context.ChapterPageTasks.Add(task);
                await _context.SaveChangesAsync();

                string detailJson = JsonSerializer.Serialize(new
                {
                    assigned_to_user_id = assignedToUserId,
                    type_code = typeCode,
                    task_title = taskTitle,
                    priority_level = priorityLevel,
                    due_at_utc = dueAtUtc,
                    compensation_amount = compensationAmount ?? 0m,
                    page_region_ids = normalizedRegionIds
                });

                await AppendTaskCreatedAuditAsync(
                    actorUserId,
                    task.ChapterPageTaskId,
                    detailJson);

                await transaction.CommitAsync();
                return task.ChapterPageTaskId;
            }
            catch (DbUpdateException ex) when (IsExpectedTaskConstraintFailure(ex))
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Expected database constraint failure while creating a chapter page task.");
                throw new InvalidOperationException(
                    "The task details conflict with the current data. Please review them and try again.");
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected database failure while creating a chapter page task.");
                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task AppendTaskCreatedAuditAsync(
            Guid actorUserId,
            Guid taskId,
            string detailJson)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "audit.usp_AuditEvent_Append";
            command.CommandType = CommandType.StoredProcedure;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

            command.Parameters.Add(new SqlParameter("@actor_user_id", SqlDbType.UniqueIdentifier) { Value = actorUserId });
            command.Parameters.Add(new SqlParameter("@action_code", SqlDbType.NVarChar, 64) { Value = "CHAPTER_PAGE_TASK_CREATED" });
            command.Parameters.Add(new SqlParameter("@entity_type", SqlDbType.NVarChar, 128) { Value = "ChapterPageTask" });
            command.Parameters.Add(new SqlParameter("@entity_id", SqlDbType.NVarChar, 100) { Value = taskId.ToString() });
            command.Parameters.Add(new SqlParameter("@detail_json", SqlDbType.NVarChar, -1) { Value = detailJson });

            await command.ExecuteNonQueryAsync();
        }

        private static bool IsExpectedTaskConstraintFailure(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlException
                && sqlException.Number is 547 or 2601 or 2627 or 2628 or 8115;
        }

        public async Task<ChapterPageTask?> GetByIdWithRegionsAsync(Guid id)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.ChapterPage)
                            .ThenInclude(p => p.Chapter)
                                .ThenInclude(c => c.Series)
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.CompletedPageVersion)
                    .ThenInclude(v => v.PageFile)
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

        // --- Reassign task to different assistant SP wrapper ---

        public async Task<Guid> AssignToDifferentUserAsync(
            Guid actorUserId,
            Guid chapterPageTaskId,
            Guid newAssignedToUserId,
            string reason,
            string updatedTaskDescription)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_ChapterPageTask_AssignToDifferentUser";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@actor_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@chapter_page_task_id", System.Data.SqlDbType.UniqueIdentifier) { Value = chapterPageTaskId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@new_assigned_to_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = newAssignedToUserId });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@reason", System.Data.SqlDbType.NVarChar, 500) { Value = reason });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@updated_task_description", System.Data.SqlDbType.NVarChar, -1)
            {
                Value = string.IsNullOrWhiteSpace(updatedTaskDescription) ? (object)DBNull.Value : updatedTaskDescription
            });

            var outParam = new Microsoft.Data.SqlClient.SqlParameter("@new_chapter_page_task_id", System.Data.SqlDbType.UniqueIdentifier) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outParam);

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();

            return outParam.Value == DBNull.Value ? Guid.Empty : (Guid)outParam.Value;
        }

        // --- Eligible assistants for task reassignment ---

        public async Task<IReadOnlyList<(Guid UserId, string DisplayName, string? Username)>> GetEligibleAssistantsForTaskAsync(Guid taskId)
        {
            // Derive seriesId and current assignee from task's region chain using explicit joins
            // to avoid CS8602 warnings from chained nullable navigation properties.
            var taskContext = await (
                from t in _context.ChapterPageTasks.AsNoTracking()
                where t.ChapterPageTaskId == taskId
                from r in t.PageRegions
                join cpv in _context.Set<Domain.Entities.ChapterPageVersion>()
                    on r.ChapterPageVersionId equals cpv.ChapterPageVersionId
                join cp in _context.Set<Domain.Entities.ChapterPage>()
                    on cpv.ChapterPageId equals cp.ChapterPageId
                join ch in _context.Set<Domain.Entities.Chapter>()
                    on cp.ChapterId equals ch.ChapterId
                select new { ch.SeriesId, t.AssignedToUserId }
            ).FirstOrDefaultAsync();

            if (taskContext == null || taskContext.SeriesId == Guid.Empty)
                return Array.Empty<(Guid, string, string?)>();

            // Query active contributors for this series with Assistant role, exclude current assignee
            var assistants = await _context.ActiveSeriesContributors
                .AsNoTracking()
                .Where(asc => asc.SeriesId == taskContext.SeriesId
                           && asc.RoleName == "Assistant"
                           && asc.UserStatusCode == "ACTIVE"
                           && asc.UserId != taskContext.AssignedToUserId)
                .Join(_context.Users,
                    asc => asc.UserId,
                    u => u.UserId,
                    (asc, u) => new { u.UserId, u.DisplayName, u.Username })
                .OrderBy(x => x.DisplayName)
                .ToListAsync();

            return assistants
                .Select(a => (a.UserId, a.DisplayName ?? a.Username, (string?)a.Username))
                .ToList();
        }
    }
}
