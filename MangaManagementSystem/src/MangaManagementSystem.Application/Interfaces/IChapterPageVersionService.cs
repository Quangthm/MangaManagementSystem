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
        /// Soft-deletes only the image (FileResource) of a page version, keeping the version row and
        /// its regions as a history placeholder (BR-CP-013/020, BR-FILE-008). The delete is refused
        /// when a region of the version is still linked to an active page task or an unresolved
        /// annotation. The returned Cloudinary public id should be best-effort removed by the caller.
        /// </summary>
        Task<DeleteVersionImageResultDto> DeleteVersionImageAsync(
            Guid versionId,
            Guid actorUserId,
            string? actorRoleName,
            CancellationToken cancellationToken = default);

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
        /// <summary>
        /// Atomically creates a FileResource, a new ChapterPage, and ChapterPageVersion (version 1)
        /// in a single database transaction.
        /// </summary>
        Task<CreatePageWithVersionResponseDto> CreatePageWithVersionAndFileAsync(
            CreatePageWithVersionRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

