using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterPageAnnotationService : IChapterPageAnnotationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterPageAnnotationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterPageAnnotationDto> CreateChapterPageAnnotationAsync(CreateChapterPageAnnotationDto dto)
        {
            // Workflow create goes through the stored procedure so permission checks,
            // same-page-version validation, transaction handling, and audit logging
            // are owned by SQL. The annotation author is the workflow actor.
            var newAnnotationId = await _unitOfWork.ChapterPageAnnotations.CreateChapterPageAnnotationAsync(
                dto.AnnotatedByUserId,
                dto.PageRegionIds,
                dto.IssueTypeCode,
                dto.AnnotationText ?? string.Empty);

            // Reload with regions
            var entity = await _unitOfWork.ChapterPageAnnotations.GetByIdWithRegionsAsync(newAnnotationId);
            return entity == null ? throw new InvalidOperationException("Failed to create annotation") : MapToDto(entity);
        }

        public async Task<ChapterPageAnnotationDto?> GetChapterPageAnnotationByIdAsync(Guid id)
        {
            // Use the Include-based read so the DTO returns populated PageRegions.
            var entity = await _unitOfWork.ChapterPageAnnotations.GetByIdWithRegionsAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<ChapterPageAnnotationDto?> GetChapterPageAnnotationByIdWithRegionsAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageAnnotations.GetByIdWithRegionsAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageAnnotationDto>> GetChapterPageAnnotationsByPageRegionIdAsync(Guid pageRegionId)
        {
            var annotations = await _unitOfWork.ChapterPageAnnotations.GetByPageRegionIdAsync(pageRegionId);
            return annotations.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ChapterPageAnnotationDto>> GetAnnotationsByPageRegionIdsAsync(IReadOnlyList<Guid> pageRegionIds)
        {
            var annotations = await _unitOfWork.ChapterPageAnnotations.GetByPageRegionIdsAsync(pageRegionIds);
            return annotations.Select(MapToDto).ToList();
        }

        public async Task<ChapterPageAnnotationDto?> UpdateChapterPageAnnotationAsync(UpdateChapterPageAnnotationDto dto)
        {
            // Load with regions so the existing PageRegions links are tracked and can be reconciled.
            var entity = await _unitOfWork.ChapterPageAnnotations.GetByIdWithRegionsAsync(dto.ChapterPageAnnotationId);
            if (entity == null)
            {
                return null;
            }

            entity.IssueTypeCode = dto.IssueTypeCode;
            entity.AnnotatedByUserId = dto.AnnotatedByUserId;
            entity.AnnotationText = dto.AnnotationText;
            entity.ResolvedByUserId = dto.ResolvedByUserId;

            entity.PageRegions.Clear();
            await AttachPageRegionsAsync(entity, dto.PageRegionIds);

            _unitOfWork.ChapterPageAnnotations.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterPageAnnotationAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageAnnotations.GetByIdWithRegionsAsync(id);
            if (entity == null)
            {
                return false;
            }

            entity.PageRegions.Clear();
            _unitOfWork.ChapterPageAnnotations.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResolveAnnotationAsync(Guid actorUserId, Guid annotationId, string? resolutionNote = null)
        {
            return await _unitOfWork.ChapterPageAnnotations.ResolveAnnotationAsync(actorUserId, annotationId, resolutionNote);
        }

        public async Task<IEnumerable<ChapterPageAnnotationDto>> GetChapterPageAnnotationsByChapterPageIdAsync(Guid chapterPageId)
        {
            var annotations = await _unitOfWork.ChapterPageAnnotations.GetByChapterPageIdWithRegionsAsync(chapterPageId);
            return annotations.Select(MapToDto).ToList();
        }

        private async Task AttachPageRegionsAsync(ChapterPageAnnotation entity, IReadOnlyList<Guid> pageRegionIds)
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

        private static ChapterPageAnnotationDto MapToDto(ChapterPageAnnotation a) => new(
            a.ChapterPageAnnotationId,
            a.IssueTypeCode,
            a.AnnotatedByUserId,
            a.AnnotationText,
            a.ResolvedByUserId,
            a.PageRegions.Select(r => new PageRegionDto(
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
            CreatedAtUtc: a.CreatedAtUtc,
            AnnotatedByDisplayName: a.AnnotatedByUser?.DisplayName,
            ResolvedAtUtc: a.ResolvedAtUtc
        );
    }
}
