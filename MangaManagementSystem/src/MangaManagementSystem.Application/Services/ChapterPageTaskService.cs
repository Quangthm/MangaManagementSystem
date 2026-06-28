using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterPageTaskService : IChapterPageTaskService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterPageTaskService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterPageTaskDto> CreateChapterPageTaskAsync(CreateChapterPageTaskDto dto)
        {
            // Workflow create goes through the stored procedure so permission checks,
            // same-page-version validation, transaction handling, and audit logging
            // are owned by SQL. The actor (task creator) is distinct from the assignee.
            var newTaskId = await _unitOfWork.ChapterPageTasks.CreateChapterPageTaskAsync(
                dto.ActorUserId,
                dto.AssignedToUserId,
                dto.TypeCode,
                dto.TaskTitle,
                dto.TaskDescription,
                (byte)dto.PriorityLevel,
                dto.DueAtUtc ?? DateTime.UtcNow.AddDays(7),
                dto.CompensationAmount,
                dto.PageRegionIds);

            // Reload with regions
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdWithRegionsAsync(newTaskId);
            return entity == null ? throw new InvalidOperationException("Failed to create task") : MapToDto(entity);
        }

        public async Task<ChapterPageTaskDto?> GetChapterPageTaskByIdAsync(Guid id)
        {
            // Use the Include-based read so the DTO returns populated PageRegions.
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdWithRegionsAsync(id);
            return entity == null ? null : MapToDtoWithFullContext(entity);
        }

        public async Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByAssignedUserIdAsync(Guid assignedToUserId)
        {
            // Filter at the SQL level (with regions) instead of loading all tasks into memory.
            var entities = await _unitOfWork.ChapterPageTasks.GetByAssignedUserIdWithRegionsAsync(assignedToUserId);
            return entities.Select(MapToDto).ToList();
        }

        public async Task<ChapterPageTaskDto?> UpdateChapterPageTaskAsync(UpdateChapterPageTaskDto dto)
        {
            // Load with regions so EF can reconcile the linked PageRegions on save.
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdWithRegionsAsync(dto.ChapterPageTaskId);
            if (entity == null)
            {
                return null;
            }

            entity.AssignedToUserId = dto.AssignedToUserId;
            entity.TypeCode = dto.TypeCode;
            entity.StatusCode = dto.StatusCode;
            entity.PriorityLevel = (byte)dto.PriorityLevel;
            entity.DueAtUtc = dto.DueAtUtc ?? entity.DueAtUtc;
            entity.CompletedPageVersionId = dto.CompletedPageVersionId;

            entity.PageRegions.Clear();
            await AttachPageRegionsAsync(entity, dto.PageRegionIds);

            _unitOfWork.ChapterPageTasks.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterPageTaskAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdWithRegionsAsync(id);
            if (entity == null)
            {
                return false;
            }

            entity.PageRegions.Clear();
            _unitOfWork.ChapterPageTasks.Delete(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<ChapterPageTaskDto?> GetChapterPageTaskByIdWithRegionsAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdWithRegionsAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByAssignedUserIdWithRegionsAsync(Guid assignedToUserId)
        {
            var entities = await _unitOfWork.ChapterPageTasks.GetByAssignedUserIdWithRegionsAsync(assignedToUserId);
            return entities.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByCreatorUserIdAsync(Guid creatorUserId)
        {
            var entities = await _unitOfWork.ChapterPageTasks.GetByCreatorUserIdWithSeriesAsync(creatorUserId);
            return entities.Select(t =>
            {
                var seriesId = t.PageRegions
                    .Select(r => r.ChapterPageVersion?.ChapterPage?.Chapter?.SeriesId)
                    .FirstOrDefault(id => id.HasValue);
                var assignedName = t.AssignedToUser?.DisplayName;
                var dto = MapToDto(t);
                return dto with { SeriesId = seriesId, AssignedToDisplayName = assignedName };
            }).ToList();
        }

        public async Task<IEnumerable<ChapterPageTaskDto>> GetAssignedTasksForAssistantAsync(Guid assistantUserId)
        {
            var entities = await _unitOfWork.ChapterPageTasks.GetByAssignedUserIdWithFullContextAsync(assistantUserId);
            return entities.Select(MapToDtoWithAssistantContext).ToList();
        }

        public async Task<ChapterPageTaskDto?> GetAssignedTaskDetailForAssistantAsync(Guid assistantUserId, Guid taskId)
        {
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdWithFullContextAsync(taskId);
            if (entity == null || entity.AssignedToUserId != assistantUserId)
            {
                return null;
            }
            return MapToDtoWithAssistantContext(entity);
        }

        public async Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByChapterPageIdAsync(Guid chapterPageId)
        {
            var entities = await _unitOfWork.ChapterPageTasks.GetByChapterPageIdWithRegionsAsync(chapterPageId);
            return entities.Select(MapToDto).ToList();
        }

        private async Task AttachPageRegionsAsync(ChapterPageTask entity, IReadOnlyList<Guid> pageRegionIds)
        {
            if (pageRegionIds == null)
            {
                return;
            }

            foreach (var pageRegionId in pageRegionIds.Distinct())
            {
                var region = await _unitOfWork.PageRegions.GetByIdAsync(pageRegionId);
                if (region != null)
                {
                    entity.PageRegions.Add(region);
                }
            }
        }

        private static ChapterPageTaskDto MapToDto(ChapterPageTask t)
        {
            return new ChapterPageTaskDto(
                t.ChapterPageTaskId,
                t.AssignedToUserId,
                t.TypeCode,
                t.StatusCode,
                (int)t.PriorityLevel,
                t.DueAtUtc,
                t.CompletedPageVersionId,
                t.TaskTitle,
                t.TaskDescription,
                t.PageRegions.Select(r => new PageRegionDto(
                    r.PageRegionId,
                    r.ChapterPageVersionId,
                    r.TypeCode,
                    r.RegionLabel,
                    r.X,
                    r.Y,
                    r.Width,
                    r.Height,
                    r.ConfidenceScore,
                    r.SourceType,
                    r.OriginalText,
                    r.CreatedByUserId,
                    r.UpdatedByUserId)).ToList(),
                AssignedToDisplayName: t.AssignedToUser?.DisplayName,
                AssignedUsername: t.AssignedToUser?.Username,
                CreatedAtUtc: t.CreatedAtUtc
            );
        }

        // --- Mangaka task lifecycle actions ---

        public async Task ApproveTaskAsync(Guid actorUserId, Guid taskId, string? completionNote)
        {
            await _unitOfWork.ChapterPageTasks.MarkTaskCompletedAsync(actorUserId, taskId, completionNote);
        }

        public async Task ReturnTaskForReworkAsync(Guid actorUserId, Guid taskId, string reason)
        {
            if (actorUserId == Guid.Empty)
                throw new InvalidOperationException("Actor user ID is required.");
            if (taskId == Guid.Empty)
                throw new InvalidOperationException("Task ID is required.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Updated task instructions are required when returning a task for rework.");

            var task = await _unitOfWork.ChapterPageTasks.GetByIdWithRegionsAsync(taskId);
            if (task == null)
                throw new InvalidOperationException("Task not found.");

            if (task.CreatedByUserId != actorUserId)
                throw new InvalidOperationException("You are not authorized to return this task for rework.");

            if (task.StatusCode != "UNDER_REVIEW")
                throw new InvalidOperationException("Only tasks currently under review can be returned for rework.");

            await _unitOfWork.ChapterPageTasks.ReturnTaskForReworkAsync(actorUserId, taskId, reason);
        }

        public async Task CancelTaskAsync(Guid actorUserId, Guid taskId, string reason)
        {
            await _unitOfWork.ChapterPageTasks.CancelTaskAsync(actorUserId, taskId, reason);
        }

        public async Task<IEnumerable<ChapterPageTaskDto>> GetTasksForReviewByCreatorAsync(Guid creatorUserId)
        {
            var entities = await _unitOfWork.ChapterPageTasks.GetTasksForReviewByCreatorAsync(creatorUserId);
            return entities.Select(MapToDtoWithFullContext).ToList();
        }

        // --- Reassignment ---

        public async Task<ReassignChapterPageTaskResult> ReassignTaskAsync(
            Guid actorUserId,
            Guid taskId,
            ReassignChapterPageTaskRequest request)
        {
            if (actorUserId == Guid.Empty)
                throw new InvalidOperationException("Actor user ID is required.");
            if (taskId == Guid.Empty)
                throw new InvalidOperationException("Task ID is required.");
            if (request.NewAssignedToUserId == Guid.Empty)
                throw new InvalidOperationException("New assigned user ID is required.");
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new InvalidOperationException("A reason is required when reassigning a task.");
            if (request.Reason.Trim().Length > 500)
                throw new InvalidOperationException("Reason must not exceed 500 characters.");

            // Load task to validate status and ownership
            var task = await _unitOfWork.ChapterPageTasks.GetByIdWithRegionsAsync(taskId);
            if (task == null)
                throw new InvalidOperationException("Task not found.");

            // Verify actor created this task
            if (task.CreatedByUserId != actorUserId)
                throw new InvalidOperationException("You are not authorized to reassign this task.");

            // Status check — only ASSIGNED and UNDER_REVIEW are reassignable
            if (task.StatusCode is "COMPLETED" or "CANCELLED")
                throw new InvalidOperationException("Completed or cancelled tasks cannot be reassigned.");

            if (task.StatusCode is not ("ASSIGNED" or "UNDER_REVIEW"))
                throw new InvalidOperationException($"Task with status '{task.StatusCode}' cannot be reassigned.");

            // Cannot reassign to the same user
            if (task.AssignedToUserId == request.NewAssignedToUserId)
                throw new InvalidOperationException("New assigned user must be different from the current assignee.");

            // Use the current task description if no updated description is provided
            var updatedDescription = string.IsNullOrWhiteSpace(request.UpdatedTaskDescription)
                ? task.TaskDescription
                : request.UpdatedTaskDescription.Trim();

            // Call SP — final guards for contributor membership, locking, and audit happen in SQL
            var newTaskId = await _unitOfWork.ChapterPageTasks.AssignToDifferentUserAsync(
                actorUserId,
                taskId,
                request.NewAssignedToUserId,
                request.Reason.Trim(),
                updatedDescription);

            return new ReassignChapterPageTaskResult(taskId, newTaskId);
        }

        public async Task<IReadOnlyList<EligibleAssistantDto>> GetEligibleAssistantsForTaskAsync(
            Guid actorUserId,
            Guid taskId)
        {
            if (actorUserId == Guid.Empty)
                throw new InvalidOperationException("Actor user ID is required.");
            if (taskId == Guid.Empty)
                throw new InvalidOperationException("Task ID is required.");

            var rawAssistants = await _unitOfWork.ChapterPageTasks.GetEligibleAssistantsForTaskAsync(taskId);
            return rawAssistants
                .Select(a => new EligibleAssistantDto(a.UserId, a.DisplayName, a.Username))
                .ToList();
        }

        private static ChapterPageTaskDto MapToDtoWithFullContext(ChapterPageTask t)
        {
            var firstRegion = t.PageRegions.FirstOrDefault();
            var pageVersion = firstRegion?.ChapterPageVersion;
            var page = pageVersion?.ChapterPage;
            var chapter = page?.Chapter;
            var series = chapter?.Series;
            var pageFile = pageVersion?.PageFile;
            var completedFile = t.CompletedPageVersion?.PageFile;

            return new ChapterPageTaskDto(
                t.ChapterPageTaskId,
                t.AssignedToUserId,
                t.TypeCode,
                t.StatusCode,
                (int)t.PriorityLevel,
                t.DueAtUtc,
                t.CompletedPageVersionId,
                t.TaskTitle,
                t.TaskDescription,
                t.PageRegions.Select(r => new PageRegionDto(
                    r.PageRegionId,
                    r.ChapterPageVersionId,
                    r.TypeCode,
                    r.RegionLabel,
                    r.X,
                    r.Y,
                    r.Width,
                    r.Height,
                    r.ConfidenceScore,
                    r.SourceType,
                    r.OriginalText,
                    r.CreatedByUserId,
                    r.UpdatedByUserId)).ToList(),
                SeriesId: series?.SeriesId ?? null,
                AssignedToDisplayName: t.AssignedToUser?.DisplayName,
                SeriesTitle: series?.Title,
                ChapterNumberLabel: chapter?.ChapterNumberLabel,
                ChapterTitle: chapter?.ChapterTitle,
                PageNo: page?.PageNo ?? null,
                PageVersionNo: pageVersion?.VersionNo ?? null,
                PageImageUrl: pageFile?.CloudinarySecureUrl,
                CompensationAmount: t.CompensationAmount,
                AssignedUsername: t.AssignedToUser?.Username,
                CompletedOutputUrl: completedFile?.CloudinarySecureUrl,
                CreatedByDisplayName: t.CreatedByUser?.DisplayName,
                CreatedAtUtc: t.CreatedAtUtc,
                UpdatedAtUtc: t.UpdatedAtUtc,
                SeriesSlug: series?.Slug,
                ChapterId: chapter?.ChapterId,
                SourceChapterPageVersionId: firstRegion?.ChapterPageVersionId
            );
        }

        private static ChapterPageTaskDto MapToDtoWithAssistantContext(ChapterPageTask t)
        {
            // Extract page context from first page region (task is for one page)
            var firstRegion = t.PageRegions.FirstOrDefault();
            var pageVersion = firstRegion?.ChapterPageVersion;
            var page = pageVersion?.ChapterPage;
            var chapter = page?.Chapter;
            var series = chapter?.Series;
            var pageFile = pageVersion?.PageFile;
            var completedFile = t.CompletedPageVersion?.PageFile;

            return new ChapterPageTaskDto(
                t.ChapterPageTaskId,
                t.AssignedToUserId,
                t.TypeCode,
                t.StatusCode,
                (int)t.PriorityLevel,
                t.DueAtUtc,
                t.CompletedPageVersionId,
                t.TaskTitle,
                t.TaskDescription,
                t.PageRegions.Select(r => new PageRegionDto(
                    r.PageRegionId,
                    r.ChapterPageVersionId,
                    r.TypeCode,
                    r.RegionLabel,
                    r.X,
                    r.Y,
                    r.Width,
                    r.Height,
                    r.ConfidenceScore,
                    r.SourceType,
                    r.OriginalText,
                    r.CreatedByUserId,
                    r.UpdatedByUserId)).ToList(),
                SeriesId: series?.SeriesId ?? null,
                AssignedToDisplayName: t.AssignedToUser?.DisplayName,
                SeriesTitle: series?.Title,
                ChapterNumberLabel: chapter?.ChapterNumberLabel,
                ChapterTitle: chapter?.ChapterTitle,
                PageNo: page?.PageNo ?? null,
                PageVersionNo: pageVersion?.VersionNo ?? null,
                PageImageUrl: pageFile?.CloudinarySecureUrl,
                CompensationAmount: t.CompensationAmount,
                AssignedUsername: t.AssignedToUser?.Username,
                CompletedOutputUrl: completedFile?.CloudinarySecureUrl,
                CreatedAtUtc: t.CreatedAtUtc,
                SeriesSlug: series?.Slug,
                ChapterId: chapter?.ChapterId,
                SourceChapterPageVersionId: firstRegion?.ChapterPageVersionId
            );
        }
    }
}
