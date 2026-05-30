using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
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
                PriorityLevel = dto.PriorityLevel,
                DueAtUtc = dto.DueAtUtc,
                CompletedPageVersionId = dto.CompletedPageVersionId
            };
            await _unitOfWork.ChapterPageTasks.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterPageTaskDto?> GetChapterPageTaskByIdAsync(long id)
        {
            var entity = await _unitOfWork.ChapterPageTasks.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByAssignedUserIdAsync(int assignedToUserId)
        {
            var all = await _unitOfWork.ChapterPageTasks.GetAllAsync();
            return all
                .Where(t => t.AssignedToUserId == assignedToUserId)
                .Select(MapToDto);
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
            entity.PriorityLevel = dto.PriorityLevel;
            entity.DueAtUtc = dto.DueAtUtc;
            entity.CompletedPageVersionId = dto.CompletedPageVersionId;
            _unitOfWork.ChapterPageTasks.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterPageTaskAsync(long id)
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

        private static ChapterPageTaskDto MapToDto(ChapterPageTask t) => new(
            t.ChapterPageTaskId,
            t.AssignedToUserId,
            t.TypeCode,
            t.StatusCode,
            t.PriorityLevel,
            t.DueAtUtc,
            t.CompletedPageVersionId
        );
    }
}
