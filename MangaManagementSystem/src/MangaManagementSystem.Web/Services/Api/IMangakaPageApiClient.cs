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
    }
}
