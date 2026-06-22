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
    public class ChapterPageService : IChapterPageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterPageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterPageDto> CreateChapterPageAsync(CreateChapterPageDto dto)
        {
            var entity = new ChapterPage
            {
                ChapterId = dto.ChapterId,
                PageNo = dto.PageNo,
                PageNotes = dto.PageNotes
            };
            await _unitOfWork.ChapterPages.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterPageDto?> GetChapterPageByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPages.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageDto>> GetChapterPagesByChapterIdAsync(Guid chapterId)
        {
            var all = await _unitOfWork.ChapterPages.GetAllAsync();
            return all
                .Where(p => p.ChapterId == chapterId && p.DeletedAtUtc == null)
                .OrderBy(p => p.PageNo)
                .Select(MapToDto);
        }

        public async Task<ChapterPageDto?> UpdateChapterPageAsync(UpdateChapterPageDto dto)
        {
            var entity = await _unitOfWork.ChapterPages.GetByIdAsync(dto.ChapterPageId);
            if (entity == null || entity.DeletedAtUtc != null)
            {
                return null;
            }

            entity.ChapterId = dto.ChapterId;
            entity.PageNo = dto.PageNo;
            entity.PageNotes = dto.PageNotes;
            _unitOfWork.ChapterPages.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterPageAsync(Guid id, Guid? deletedByUserId = null)
        {
            var entity = await _unitOfWork.ChapterPages.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            var tasks = await _unitOfWork.ChapterPageTasks.GetByChapterPageIdWithRegionsAsync(id);
            foreach (var task in tasks)
            {
                task.PageRegions.Clear();
                _unitOfWork.ChapterPageTasks.Delete(task);
            }

            var annotations = await _unitOfWork.ChapterPageAnnotations.GetByChapterPageIdWithRegionsAsync(id);
            foreach (var annotation in annotations)
            {
                annotation.PageRegions.Clear();
                _unitOfWork.ChapterPageAnnotations.Delete(annotation);
            }

            _unitOfWork.ChapterPages.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ChapterPageDto MapToDto(ChapterPage p) => new(
            p.ChapterPageId,
            p.ChapterId,
            p.PageNo,
            p.PageNotes,
            p.DeletedAtUtc,
            p.DeletedByUserId
        );
    }
}
