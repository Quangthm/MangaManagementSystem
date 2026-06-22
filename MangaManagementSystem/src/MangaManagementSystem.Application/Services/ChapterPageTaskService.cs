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
            var entity = new ChapterPageTask
            {
                AssignedToUserId = dto.AssignedToUserId,
                TypeCode = dto.TypeCode,
                StatusCode = dto.StatusCode,
                PriorityLevel = (byte)dto.PriorityLevel,
                DueAtUtc = dto.DueAtUtc ?? DateTime.UtcNow,
                CompletedPageVersionId = dto.CompletedPageVersionId,
            };
            await _unitOfWork.ChapterPageTasks.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterPageTaskDto?> GetChapterPageTaskByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByAssignedUserIdAsync(Guid assignedToUserId)
        {
            var all = await _unitOfWork.ChapterPageTasks.GetAllAsync();
            return all
                .Where(t => t.AssignedToUserId == assignedToUserId)
                .Select(MapToDto).ToList();
        }

        public async Task<ChapterPageTaskDto?> UpdateChapterPageTaskAsync(UpdateChapterPageTaskDto dto)
        {
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdAsync(dto.ChapterPageTaskId);
            if (entity == null)
            {
                return null;
            }

            // Update fields from DTO
            entity.AssignedToUserId = dto.AssignedToUserId;
            entity.TypeCode = dto.TypeCode;
            entity.StatusCode = dto.StatusCode;
            entity.PriorityLevel = (byte)dto.PriorityLevel;
            entity.DueAtUtc = dto.DueAtUtc ?? entity.DueAtUtc;
            entity.CompletedPageVersionId = dto.CompletedPageVersionId;
            _unitOfWork.ChapterPageTasks.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterPageTaskAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _unitOfWork.ChapterPageTasks.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
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
                t.CompletedPageVersionId
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
                    r.UpdatedByUserId)).ToList()
            );
        }

        // --- Mangaka task lifecycle actions ---

        public async Task ApproveTaskAsync(Guid actorUserId, Guid taskId, string? completionNote)
        {
            await _unitOfWork.ChapterPageTasks.MarkTaskCompletedAsync(actorUserId, taskId, completionNote);
        }

        public async Task ReturnTaskForReworkAsync(Guid actorUserId, Guid taskId, string reason)
        {
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
                SeriesSlug: series?.Slug,
                AssignedToDisplayName: t.AssignedToUser?.DisplayName,
                SeriesTitle: series?.Title,
                ChapterNumberLabel: chapter?.ChapterNumberLabel,
                ChapterTitle: chapter?.ChapterTitle,
                PageNo: page?.PageNo ?? null,
                PageImageUrl: pageFile?.CloudinarySecureUrl,
                CompensationAmount: t.CompensationAmount,
                AssignedUsername: t.AssignedToUser?.Username,
                CompletedOutputUrl: completedFile?.CloudinarySecureUrl,
                CreatedByDisplayName: t.CreatedByUser?.DisplayName,
                CreatedAtUtc: t.CreatedAtUtc,
                UpdatedAtUtc: t.UpdatedAtUtc
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
                SeriesSlug: series?.Slug,
                AssignedToDisplayName: t.AssignedToUser?.DisplayName,
                SeriesTitle: series?.Title,
                ChapterNumberLabel: chapter?.ChapterNumberLabel,
                ChapterTitle: chapter?.ChapterTitle,
                PageNo: page?.PageNo ?? null,
                PageImageUrl: pageFile?.CloudinarySecureUrl,
                CompensationAmount: t.CompensationAmount,
                AssignedUsername: t.AssignedToUser?.Username,
                CompletedOutputUrl: completedFile?.CloudinarySecureUrl
            );
        }
    }
}
