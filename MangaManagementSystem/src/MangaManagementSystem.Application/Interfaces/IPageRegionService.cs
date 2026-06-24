using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IPageRegionService
    {
        Task<PageRegionDto> CreatePageRegionAsync(CreatePageRegionDto dto);
        Task<PageRegionDto?> GetPageRegionByIdAsync(Guid id);
        Task<IEnumerable<PageRegionDto>> GetPageRegionsByChapterPageVersionIdAsync(Guid chapterPageVersionId);
        Task<PageRegionDto?> UpdatePageRegionAsync(UpdatePageRegionDto dto);
        Task<bool> DeletePageRegionAsync(Guid id);
        Task<bool> BulkReplacePageRegionsAsync(Guid chapterPageVersionId, IEnumerable<CreatePageRegionDto> dtos);
    }
}
