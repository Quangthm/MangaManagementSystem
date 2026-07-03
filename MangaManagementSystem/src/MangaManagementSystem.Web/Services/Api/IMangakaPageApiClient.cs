using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// Typed Web-to-API client for Mangaka chapter-page reads and simple edits. Page creation is part
    /// of the page+version+file workflow and is not exposed here.
    /// </summary>
    public interface IMangakaPageApiClient
    {
        Task<IReadOnlyList<ChapterPageDto>> GetByChapterAsync(Guid actorUserId, Guid chapterId, CancellationToken cancellationToken = default);
        Task<ChapterPageDto?> GetByIdAsync(Guid actorUserId, Guid pageId, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(Guid actorUserId, IReadOnlyList<Guid> chapterIds, CancellationToken cancellationToken = default);
        Task<ChapterPageDto?> UpdateNotesAsync(Guid actorUserId, Guid pageId, string? pageNotes, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid actorUserId, Guid pageId, CancellationToken cancellationToken = default);

        Task<CreatePageWithVersionResponseDto?> CreatePageWithVersionAsync(Guid actorUserId, CreatePageWithVersionRequestDto request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChapterPageVersionDto>> GetVersionsByPageIdsAsync(Guid actorUserId, IReadOnlyList<Guid> pageIds, CancellationToken cancellationToken = default);
        Task<ChapterPageVersionDto?> GetVersionByIdAsync(Guid actorUserId, Guid versionId, CancellationToken cancellationToken = default);
        Task<ChapterPageVersionDto?> CreateVersionWithFileAndRegionsAsync(Guid actorUserId, CreateVersionWithFileAndRegionsRequestDto request, CancellationToken cancellationToken = default);
        Task<ChapterPageVersionDto?> UpdateVersionAsync(Guid actorUserId, UpdateChapterPageVersionDto request, CancellationToken cancellationToken = default);
        Task<bool> SetCurrentVersionAsync(Guid actorUserId, Guid pageId, Guid versionId, CancellationToken cancellationToken = default);
        Task<DeleteVersionImageResultDto?> DeleteVersionImageAsync(Guid actorUserId, Guid versionId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileResourceDto>> GetFileResourcesByIdsAsync(Guid actorUserId, IReadOnlyList<Guid> fileIds, CancellationToken cancellationToken = default);
    }
}

