using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterPageTaskRegionService : IChapterPageTaskRegionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterPageTaskRegionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterPageTaskRegionDto> CreateChapterPageTaskRegionAsync(CreateChapterPageTaskRegionDto dto)
        {
            var entity = new ChapterPageTaskRegion
            {
                ChapterPageTaskId = dto.ChapterPageTaskId,
                PageRegionId = dto.PageRegionId
            };
            await _unitOfWork.ChapterPageTaskRegions.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterPageTaskRegionDto?> GetChapterPageTaskRegionByIdAsync(long id)
        {
            var entity = await _unitOfWork.ChapterPageTaskRegions.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageTaskRegionDto>> GetChapterPageTaskRegionsByTaskIdAsync(long chapterPageTaskId)
        {
            var all = await _unitOfWork.ChapterPageTaskRegions.GetAllAsync();
            return all
                .Where(tr => tr.ChapterPageTaskId == chapterPageTaskId)
                .Select(MapToDto);
        }

        public async Task<IEnumerable<ChapterPageTaskRegionDto>> GetChapterPageTaskRegionsByPageRegionIdAsync(long pageRegionId)
        {
            var all = await _unitOfWork.ChapterPageTaskRegions.GetAllAsync();
            return all
                .Where(tr => tr.PageRegionId == pageRegionId)
                .Select(MapToDto);
        }

        public async Task<ChapterPageTaskRegionDto?> UpdateChapterPageTaskRegionAsync(UpdateChapterPageTaskRegionDto dto)
        {
            var entity = await _unitOfWork.ChapterPageTaskRegions.GetByIdAsync(dto.ChapterPageTaskRegionId);
            if (entity == null)
            {
                return null;
            }

            entity.ChapterPageTaskId = dto.ChapterPageTaskId;
            entity.PageRegionId = dto.PageRegionId;
            _unitOfWork.ChapterPageTaskRegions.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterPageTaskRegionAsync(long id)
        {
            var entity = await _unitOfWork.ChapterPageTaskRegions.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _unitOfWork.ChapterPageTaskRegions.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ChapterPageTaskRegionDto MapToDto(ChapterPageTaskRegion tr) => new(
            tr.ChapterPageTaskRegionId,
            tr.ChapterPageTaskId,
            tr.PageRegionId
        );
    }
}
