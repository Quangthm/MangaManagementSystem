using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageTaskRegionService
    {
        Task<ChapterPageTaskRegionDto> CreateChapterPageTaskRegionAsync(CreateChapterPageTaskRegionDto dto);
        Task<ChapterPageTaskRegionDto?> GetChapterPageTaskRegionByIdAsync(long id);
        Task<IEnumerable<ChapterPageTaskRegionDto>> GetChapterPageTaskRegionsByTaskIdAsync(long chapterPageTaskId);
        Task<IEnumerable<ChapterPageTaskRegionDto>> GetChapterPageTaskRegionsByPageRegionIdAsync(long pageRegionId);
        Task<ChapterPageTaskRegionDto?> UpdateChapterPageTaskRegionAsync(UpdateChapterPageTaskRegionDto dto);
        Task<bool> DeleteChapterPageTaskRegionAsync(long id);
    }
}
