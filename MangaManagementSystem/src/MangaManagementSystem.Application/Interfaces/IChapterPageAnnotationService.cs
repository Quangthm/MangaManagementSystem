using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageAnnotationService
    {
        Task<ChapterPageAnnotationDto> CreateChapterPageAnnotationAsync(CreateChapterPageAnnotationDto dto);
        Task<ChapterPageAnnotationDto?> GetChapterPageAnnotationByIdAsync(long id);
        Task<IEnumerable<ChapterPageAnnotationDto>> GetChapterPageAnnotationsByPageRegionIdAsync(long pageRegionId);
        Task<ChapterPageAnnotationDto?> UpdateChapterPageAnnotationAsync(UpdateChapterPageAnnotationDto dto);
        Task<bool> DeleteChapterPageAnnotationAsync(long id);
    }
}
