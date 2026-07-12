using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces;

public interface IPageRegionService
{
<<<<<<< HEAD
    Task<PageRegionDto> CreatePageRegionAsync(CreatePageRegionDto dto);
    Task<PageRegionDto?> GetPageRegionByIdAsync(Guid id);
    Task<IEnumerable<PageRegionDto>> GetPageRegionsByChapterPageVersionIdAsync(Guid chapterPageVersionId);
    Task<IEnumerable<PageRegionDto>> GetPageRegionsByVersionIdsAsync(IEnumerable<Guid> chapterPageVersionIds);
    Task<Dictionary<Guid, int>> GetRegionCountsByVersionIdsAsync(IEnumerable<Guid> chapterPageVersionIds);
    Task<PageRegionDto?> UpdatePageRegionAsync(UpdatePageRegionDto dto);
    Task<bool> DeletePageRegionAsync(Guid id);
    Task<bool> BulkReplacePageRegionsAsync(Guid chapterPageVersionId, IEnumerable<CreatePageRegionDto> dtos);
=======
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

        /// <summary>
        /// Returns the existing whole-page (FULL_PAGE) region for the version, or creates one covering
        /// the whole page (BR-REG-031/032). Used as the default anchor when a task or annotation is
        /// created without an explicit region selection.
        /// </summary>
        Task<PageRegionDto> EnsureFullPageRegionAsync(Guid chapterPageVersionId, Guid actorUserId, CancellationToken cancellationToken = default);
    }
>>>>>>> main
}
