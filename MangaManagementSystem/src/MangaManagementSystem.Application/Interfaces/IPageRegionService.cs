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

        /// <summary>
        /// Resolves the owning chapter's current status_code for a page version (version → page → chapter),
        /// or null if the version/page/chapter no longer exists. Used by the region write endpoints to
        /// enforce the chapter-state rule (BR-REG-035/036, BR-WORKSPACE-014) server-side, since the region
        /// SPs/EF path themselves carry no status guard.
        /// </summary>
        Task<string?> GetChapterStatusByVersionIdAsync(Guid chapterPageVersionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Same as <see cref="GetChapterStatusByVersionIdAsync"/> but starting from a region
        /// (region → version → page → chapter). Used by the annotation write endpoints, which identify
        /// their target by region rather than by version, to enforce BR-ANN-013/027 server-side.
        /// </summary>
        Task<string?> GetChapterStatusByRegionIdAsync(Guid pageRegionId, CancellationToken cancellationToken = default);
    }
}
