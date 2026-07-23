using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// Typed Web-to-API client for Mangaka chapter-page reads and simple edits. Page creation is part
    /// of the page+version+file workflow and is not exposed here.
    /// </summary>
    public interface IMangakaPageApiClient
    {
        Task<IReadOnlyList<ChapterPageDto>> GetByChapterAsync(Guid chapterId, CancellationToken cancellationToken = default);
        Task<ChapterPageDto?> GetByIdAsync(Guid pageId, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(IReadOnlyList<Guid> chapterIds, CancellationToken cancellationToken = default);
        Task<ChapterPageDto?> UpdateNotesAsync(Guid pageId, string? pageNotes, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid pageId, CancellationToken cancellationToken = default);

        Task<CreatePageWithVersionResponseDto?> CreatePageWithVersionAsync(CreatePageWithVersionRequestDto request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChapterPageVersionDto>> GetVersionsByPageIdsAsync(IReadOnlyList<Guid> pageIds, CancellationToken cancellationToken = default);
        Task<ChapterPageVersionDto?> GetVersionByIdAsync(Guid versionId, CancellationToken cancellationToken = default);
        Task<ChapterPageVersionDto?> CreateVersionWithFileAndRegionsAsync(CreateVersionWithFileAndRegionsRequestDto request, CancellationToken cancellationToken = default);
        Task<ChapterPageVersionDto?> UpdateVersionAsync(UpdateChapterPageVersionDto request, CancellationToken cancellationToken = default);
        Task<bool> SetCurrentVersionAsync(Guid pageId, Guid versionId, CancellationToken cancellationToken = default);
        Task<DeleteVersionImageResultDto?> DeleteVersionImageAsync(Guid versionId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileResourceDto>> GetFileResourcesByIdsAsync(IReadOnlyList<Guid> fileIds, CancellationToken cancellationToken = default);
    }
}

