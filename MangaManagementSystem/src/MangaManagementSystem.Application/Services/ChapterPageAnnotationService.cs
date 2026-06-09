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
            var entity = new ChapterPageAnnotation
            {
                PageRegionId = dto.PageRegionId,
                IssueTypeCode = dto.IssueTypeCode,
                AnnotatedByUserId = dto.AnnotatedByUserId,
                AnnotationText = dto.AnnotationText
            };
            await _unitOfWork.ChapterPageAnnotations.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterPageAnnotationDto?> GetChapterPageAnnotationByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageAnnotations.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageAnnotationDto>> GetChapterPageAnnotationsByPageRegionIdAsync(Guid pageRegionId)
        {
            var all = await _unitOfWork.ChapterPageAnnotations.GetAllAsync();
            return all
                .Where(a => a.PageRegionId == pageRegionId)
                .Select(MapToDto);
        }

        public async Task<ChapterPageAnnotationDto?> UpdateChapterPageAnnotationAsync(UpdateChapterPageAnnotationDto dto)
        {
            var entity = await _unitOfWork.ChapterPageAnnotations.GetByIdAsync(dto.ChapterPageAnnotationId);
            if (entity == null)
            {
                return null;
            }

            entity.PageRegionId = dto.PageRegionId;
            entity.IssueTypeCode = dto.IssueTypeCode;
            entity.AnnotatedByUserId = dto.AnnotatedByUserId;
            entity.AnnotationText = dto.AnnotationText;
            entity.ResolvedByUserId = dto.ResolvedByUserId;
            _unitOfWork.ChapterPageAnnotations.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterPageAnnotationAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageAnnotations.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _unitOfWork.ChapterPageAnnotations.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ChapterPageAnnotationDto MapToDto(ChapterPageAnnotation a) => new(
            a.ChapterPageAnnotationId,
            a.PageRegionId,
            a.IssueTypeCode,
            a.AnnotatedByUserId,
            a.AnnotationText,
            a.ResolvedByUserId
        );
    }
}
