using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageTaskRegionService
    {
        Task<ChapterPageTaskRegionDto> CreateChapterPageTaskRegionAsync(CreateChapterPageTaskRegionDto dto);
        Task<ChapterPageTaskRegionDto?> GetChapterPageTaskRegionByIdAsync(Guid id);
        Task<IEnumerable<ChapterPageTaskRegionDto>> GetChapterPageTaskRegionsByTaskIdAsync(Guid chapterPageTaskId);
        Task<IEnumerable<ChapterPageTaskRegionDto>> GetChapterPageTaskRegionsByPageRegionIdAsync(Guid pageRegionId);
        Task<ChapterPageTaskRegionDto?> UpdateChapterPageTaskRegionAsync(UpdateChapterPageTaskRegionDto dto);
        Task<bool> DeleteChapterPageTaskRegionAsync(Guid id);
    }
}
