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
    public class PageRegionService : IPageRegionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PageRegionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PageRegionDto> CreatePageRegionAsync(CreatePageRegionDto dto)
        {
            var entity = new PageRegion
            {
                ChapterPageVersionId = dto.ChapterPageVersionId,
                TypeCode = dto.TypeCode,
                RegionLabel = dto.RegionLabel,
                X = dto.X,
                Y = dto.Y,
                Width = dto.Width,
                Height = dto.Height,
                ConfidenceScore = dto.ConfidenceScore,
                SourceType = dto.SourceType,
                OriginalText = dto.OriginalText,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _unitOfWork.PageRegions.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<PageRegionDto?> GetPageRegionByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.PageRegions.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<PageRegionDto>> GetPageRegionsByChapterPageVersionIdAsync(Guid chapterPageVersionId)
        {
            var all = await _unitOfWork.PageRegions.GetAllAsync();
            return all
                .Where(r => r.ChapterPageVersionId == chapterPageVersionId)
                .Select(MapToDto);
        }

        public async Task<PageRegionDto?> UpdatePageRegionAsync(UpdatePageRegionDto dto)
        {
            var entity = await _unitOfWork.PageRegions.GetByIdAsync(dto.PageRegionId);
            if (entity == null)
            {
                return null;
            }

            entity.ChapterPageVersionId = dto.ChapterPageVersionId;
            entity.TypeCode = dto.TypeCode;
            entity.RegionLabel = dto.RegionLabel;
            entity.X = dto.X;
            entity.Y = dto.Y;
            entity.Width = dto.Width;
            entity.Height = dto.Height;
            entity.ConfidenceScore = dto.ConfidenceScore;
            entity.SourceType = dto.SourceType;
            entity.OriginalText = dto.OriginalText;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            _unitOfWork.PageRegions.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeletePageRegionAsync(Guid id)
        {
            var entity = await _unitOfWork.PageRegions.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _unitOfWork.PageRegions.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BulkReplacePageRegionsAsync(Guid chapterPageVersionId, IEnumerable<CreatePageRegionDto> dtos)
        {
            // Get all existing regions for this version
            var all = await _unitOfWork.PageRegions.GetAllAsync();
            var existing = all.Where(r => r.ChapterPageVersionId == chapterPageVersionId).ToList();

            // Create or update
            foreach (var dto in dtos)
            {
                var existingRegion = string.IsNullOrEmpty(dto.RegionLabel) 
                    ? null 
                    : existing.FirstOrDefault(r => r.RegionLabel == dto.RegionLabel);

                if (existingRegion != null)
                {
                    // Update
                    existingRegion.TypeCode = dto.TypeCode;
                    existingRegion.X = dto.X;
                    existingRegion.Y = dto.Y;
                    existingRegion.Width = dto.Width;
                    existingRegion.Height = dto.Height;
                    existingRegion.ConfidenceScore = dto.ConfidenceScore;
                    existingRegion.SourceType = dto.SourceType;
                    existingRegion.OriginalText = dto.OriginalText;
                    _unitOfWork.PageRegions.Update(existingRegion);
                    existing.Remove(existingRegion);
                }
                else
                {
                    // Create new
                    var entity = new PageRegion
                    {
                        ChapterPageVersionId = chapterPageVersionId,
                        TypeCode = dto.TypeCode,
                        RegionLabel = dto.RegionLabel,
                        X = dto.X,
                        Y = dto.Y,
                        Width = dto.Width,
                        Height = dto.Height,
                        ConfidenceScore = dto.ConfidenceScore,
                        SourceType = dto.SourceType,
                        OriginalText = dto.OriginalText,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    await _unitOfWork.PageRegions.AddAsync(entity);
                }
            }

            // Delete remaining (which were not in the new dtos)
            foreach (var r in existing)
            {
                _unitOfWork.PageRegions.Delete(r);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static PageRegionDto MapToDto(PageRegion r) => new(
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
            r.UpdatedByUserId
        );
    }
}
