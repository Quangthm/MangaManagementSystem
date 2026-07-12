using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>Typed Web-to-API client for Mangaka page-region reads/writes.</summary>
    public interface IMangakaPageRegionApiClient
    {
        Task<PageRegionDto> CreateAsync(Guid actorUserId, CreatePageRegionDto dto, CancellationToken cancellationToken = default);
        Task<PageRegionDto> EnsureFullPageRegionAsync(Guid actorUserId, Guid versionId, CancellationToken cancellationToken = default);
        Task BulkReplaceAsync(Guid actorUserId, Guid versionId, IReadOnlyList<CreatePageRegionDto> regions, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PageRegionDto>> GetByVersionsAsync(Guid actorUserId, IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(Guid actorUserId, IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default);
    }
}
