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

            await AttachPageRegionsAsync(entity, dto.PageRegionIds);

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
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _unitOfWork.ChapterPageTasks.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
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
    }
}
