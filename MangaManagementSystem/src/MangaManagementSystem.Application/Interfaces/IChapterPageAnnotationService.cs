using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageAnnotationService
    {
        Task<ChapterPageAnnotationDto> CreateChapterPageAnnotationAsync(CreateChapterPageAnnotationDto dto);
        Task<ChapterPageAnnotationDto?> GetChapterPageAnnotationByIdAsync(Guid id);
        Task<ChapterPageAnnotationDto?> GetChapterPageAnnotationByIdWithRegionsAsync(Guid id);
        Task<IEnumerable<ChapterPageAnnotationDto>> GetChapterPageAnnotationsByPageRegionIdAsync(Guid pageRegionId);
        Task<IEnumerable<ChapterPageAnnotationDto>> GetAnnotationsByPageRegionIdsAsync(IReadOnlyList<Guid> pageRegionIds);
        Task<ChapterPageAnnotationDto?> UpdateChapterPageAnnotationAsync(UpdateChapterPageAnnotationDto dto);
        Task<bool> DeleteChapterPageAnnotationAsync(Guid id);
        Task<bool> ResolveAnnotationAsync(Guid actorUserId, Guid annotationId, string? resolutionNote = null);
        Task<IEnumerable<ChapterPageAnnotationDto>> GetChapterPageAnnotationsByChapterPageIdAsync(Guid chapterPageId);
    }
}
