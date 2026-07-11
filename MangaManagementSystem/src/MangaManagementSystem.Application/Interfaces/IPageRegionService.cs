using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces;

public interface IPageRegionService
{
    Task<PageRegionDto> CreatePageRegionAsync(CreatePageRegionDto dto);
    Task<PageRegionDto?> GetPageRegionByIdAsync(Guid id);
    Task<IEnumerable<PageRegionDto>> GetPageRegionsByChapterPageVersionIdAsync(Guid chapterPageVersionId);
    Task<IEnumerable<PageRegionDto>> GetPageRegionsByVersionIdsAsync(IEnumerable<Guid> chapterPageVersionIds);
    Task<Dictionary<Guid, int>> GetRegionCountsByVersionIdsAsync(IEnumerable<Guid> chapterPageVersionIds);
    Task<PageRegionDto?> UpdatePageRegionAsync(UpdatePageRegionDto dto);
    Task<bool> DeletePageRegionAsync(Guid id);
    Task<bool> BulkReplacePageRegionsAsync(Guid chapterPageVersionId, IEnumerable<CreatePageRegionDto> dtos);
}
