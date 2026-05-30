using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IPageRegionService
    {
        Task<PageRegionDto> CreatePageRegionAsync(CreatePageRegionDto dto);
        Task<PageRegionDto?> GetPageRegionByIdAsync(long id);
        Task<IEnumerable<PageRegionDto>> GetPageRegionsByChapterPageVersionIdAsync(long chapterPageVersionId);
        Task<PageRegionDto?> UpdatePageRegionAsync(UpdatePageRegionDto dto);
        Task<bool> DeletePageRegionAsync(long id);
    }
}
