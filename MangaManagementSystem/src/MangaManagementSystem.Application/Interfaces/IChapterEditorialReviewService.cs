using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterEditorialReviewService
    {
        Task<ChapterEditorialReviewDto> CreateChapterEditorialReviewAsync(CreateChapterEditorialReviewDto dto);
        Task<ChapterEditorialReviewDto?> GetChapterEditorialReviewByIdAsync(Guid id);
        Task<IEnumerable<ChapterEditorialReviewDto>> GetChapterEditorialReviewsByChapterIdAsync(Guid chapterId);
        Task<ChapterEditorialReviewDto?> UpdateChapterEditorialReviewAsync(UpdateChapterEditorialReviewDto dto);
        Task<bool> DeleteChapterEditorialReviewAsync(Guid id);
    }
}
