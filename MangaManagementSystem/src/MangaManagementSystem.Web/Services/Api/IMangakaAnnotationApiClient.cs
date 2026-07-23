using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>Typed Web-to-API client for Mangaka page annotations.</summary>
    public interface IMangakaAnnotationApiClient
    {
        Task<IReadOnlyList<ChapterPageAnnotationDto>> GetByPageAsync(Guid chapterPageId, CancellationToken cancellationToken = default);
        Task<ChapterPageAnnotationDto> CreateAsync(CreateMangakaAnnotationRequest request, CancellationToken cancellationToken = default);
        Task<bool> ResolveAsync(Guid annotationId, string? resolutionNote = null, CancellationToken cancellationToken = default);
    }
}
