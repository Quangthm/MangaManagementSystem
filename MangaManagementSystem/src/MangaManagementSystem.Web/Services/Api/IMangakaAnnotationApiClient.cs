using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>Typed Web-to-API client for Mangaka page annotations.</summary>
    public interface IMangakaAnnotationApiClient
    {
        Task<IReadOnlyList<ChapterPageAnnotationDto>> GetByPageAsync(Guid actorUserId, Guid chapterPageId, CancellationToken cancellationToken = default);
        Task<ChapterPageAnnotationDto> CreateAsync(Guid actorUserId, CreateMangakaAnnotationRequest request, CancellationToken cancellationToken = default);
        Task<bool> ResolveAsync(Guid actorUserId, Guid annotationId, string? resolutionNote = null, CancellationToken cancellationToken = default);
    }
}
