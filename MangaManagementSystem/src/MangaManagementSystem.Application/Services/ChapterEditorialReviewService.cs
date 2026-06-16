using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterEditorialReviewService : IChapterEditorialReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterEditorialReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterEditorialReviewDto> CreateChapterEditorialReviewAsync(CreateChapterEditorialReviewDto dto)
        {
            var entity = new ChapterEditorialReview
            {
                ChapterId = dto.ChapterId,
                ReviewerUserId = dto.ReviewerUserId,
                DecisionCode = dto.DecisionCode,
                Feedback = dto.Feedback,
                MarkupFileId = dto.MarkupFileId
            };
            await _unitOfWork.ChapterEditorialReviews.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterEditorialReviewDto?> GetChapterEditorialReviewByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterEditorialReviews.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterEditorialReviewDto>> GetChapterEditorialReviewsByChapterIdAsync(Guid chapterId)
        {
            var all = await _unitOfWork.ChapterEditorialReviews.GetAllAsync();
            return all
                .Where(r => r.ChapterId == chapterId)
                .Select(MapToDto);
        }

        public async Task<ChapterEditorialReviewDto?> UpdateChapterEditorialReviewAsync(UpdateChapterEditorialReviewDto dto)
        {
            var entity = await _unitOfWork.ChapterEditorialReviews.GetByIdAsync(dto.ChapterEditorialReviewId);
            if (entity == null)
            {
                return null;
            }

            entity.ChapterId = dto.ChapterId;
            entity.ReviewerUserId = dto.ReviewerUserId;
            entity.DecisionCode = dto.DecisionCode;
            entity.Feedback = dto.Feedback;
            entity.MarkupFileId = dto.MarkupFileId;
            _unitOfWork.ChapterEditorialReviews.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterEditorialReviewAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterEditorialReviews.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _unitOfWork.ChapterEditorialReviews.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ChapterEditorialReviewDto MapToDto(ChapterEditorialReview r) => new(
            r.ChapterEditorialReviewId,
            r.ChapterId,
            r.ReviewerUserId,
            r.DecisionCode,
            r.Feedback,
            r.MarkupFileId
        );
    }
}
