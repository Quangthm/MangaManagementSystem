using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageVersionService
    {
        Task<ChapterPageVersionDto> CreateChapterPageVersionAsync(CreateChapterPageVersionDto dto);
        Task<ChapterPageVersionDto?> GetChapterPageVersionByIdAsync(Guid id);
        Task<IEnumerable<ChapterPageVersionDto>> GetChapterPageVersionsByChapterPageIdAsync(Guid chapterPageId);
        Task<IEnumerable<ChapterPageVersionDto>> GetChapterPageVersionsByPageIdsAsync(IEnumerable<Guid> chapterPageIds);
        Task<ChapterPageVersionDto?> UpdateChapterPageVersionAsync(UpdateChapterPageVersionDto dto);
        Task<bool> DeleteChapterPageVersionAsync(Guid id);
        Task<bool> SetCurrentVersionAsync(Guid chapterPageId, Guid chapterPageVersionId);

        /// <summary>
        /// Atomically creates a FileResource, a new ChapterPageVersion that references it, its page
        /// regions, and (optionally) marks it the current version — all in one DB transaction.
        /// The Cloudinary upload must already have happened; the caller is responsible for best-effort
        /// Cloudinary cleanup if this throws.
        /// </summary>
        Task<ChapterPageVersionDto> CreateVersionWithFileAndRegionsAsync(
            Guid chapterPageId,
            short versionNo,
            CreateFileResourceDto fileDto,
            string? versionNote,
            IEnumerable<CreatePageRegionDto> regionDtos,
            bool setAsCurrent,
            CancellationToken cancellationToken = default);
    }
}
