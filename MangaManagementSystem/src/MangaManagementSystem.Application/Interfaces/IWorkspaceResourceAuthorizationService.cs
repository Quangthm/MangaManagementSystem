namespace MangaManagementSystem.Application.Interfaces;

public interface IWorkspaceResourceAuthorizationService
{
    Task<bool> CanAccessSeriesAsync(Guid actorUserId, Guid seriesId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessChaptersAsync(Guid actorUserId, IEnumerable<Guid> chapterIds, CancellationToken cancellationToken = default);
    Task<bool> CanAccessPagesAsync(Guid actorUserId, IEnumerable<Guid> pageIds, CancellationToken cancellationToken = default);
    Task<bool> CanAccessVersionsAsync(Guid actorUserId, IEnumerable<Guid> versionIds, CancellationToken cancellationToken = default);
    Task<bool> CanAccessFilesAsync(Guid actorUserId, IEnumerable<Guid> fileIds, CancellationToken cancellationToken = default);
    Task<bool> CanAccessRegionsAsync(Guid actorUserId, IEnumerable<Guid> regionIds, CancellationToken cancellationToken = default);
    Task<bool> CanAccessAnnotationAsync(Guid actorUserId, Guid annotationId, CancellationToken cancellationToken = default);
}
