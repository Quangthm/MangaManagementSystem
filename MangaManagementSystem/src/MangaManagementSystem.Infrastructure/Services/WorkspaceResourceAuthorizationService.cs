using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Services;

public sealed class WorkspaceResourceAuthorizationService : IWorkspaceResourceAuthorizationService
{
    private readonly ApplicationDbContext _dbContext;

    public WorkspaceResourceAuthorizationService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> CanAccessSeriesAsync(Guid actorUserId, Guid seriesId, CancellationToken cancellationToken = default) =>
        HasSeriesAccessAsync(actorUserId, seriesId, cancellationToken);

    public async Task<bool> CanAccessChaptersAsync(Guid actorUserId, IEnumerable<Guid> chapterIds, CancellationToken cancellationToken = default)
    {
        var ids = Normalize(chapterIds);
        var resources = await _dbContext.Chapters.AsNoTracking()
            .Where(x => ids.Contains(x.ChapterId))
            .Select(x => new { Id = x.ChapterId, x.SeriesId })
            .ToListAsync(cancellationToken);
        return await ValidateAsync(actorUserId, ids, resources.Select(x => (x.Id, x.SeriesId)), cancellationToken);
    }

    public async Task<bool> CanAccessPagesAsync(Guid actorUserId, IEnumerable<Guid> pageIds, CancellationToken cancellationToken = default)
    {
        var ids = Normalize(pageIds);
        var resources = await _dbContext.ChapterPages.AsNoTracking()
            .Where(x => ids.Contains(x.ChapterPageId))
            .Select(x => new { Id = x.ChapterPageId, x.Chapter!.SeriesId })
            .ToListAsync(cancellationToken);
        return await ValidateAsync(actorUserId, ids, resources.Select(x => (x.Id, x.SeriesId)), cancellationToken);
    }

    public async Task<bool> CanAccessVersionsAsync(Guid actorUserId, IEnumerable<Guid> versionIds, CancellationToken cancellationToken = default)
    {
        var ids = Normalize(versionIds);
        var resources = await _dbContext.ChapterPageVersions.AsNoTracking()
            .Where(x => ids.Contains(x.ChapterPageVersionId))
            .Select(x => new { Id = x.ChapterPageVersionId, x.ChapterPage!.Chapter!.SeriesId })
            .ToListAsync(cancellationToken);
        return await ValidateAsync(actorUserId, ids, resources.Select(x => (x.Id, x.SeriesId)), cancellationToken);
    }

    public async Task<bool> CanAccessFilesAsync(Guid actorUserId, IEnumerable<Guid> fileIds, CancellationToken cancellationToken = default)
    {
        var ids = Normalize(fileIds);
        var resources = await _dbContext.ChapterPageVersions.AsNoTracking()
            .Where(x => ids.Contains(x.PageFileId))
            .Select(x => new { Id = x.PageFileId, x.ChapterPage!.Chapter!.SeriesId })
            .ToListAsync(cancellationToken);
        return await ValidateAsync(actorUserId, ids, resources.Select(x => (x.Id, x.SeriesId)), cancellationToken);
    }

    public async Task<bool> CanAccessRegionsAsync(Guid actorUserId, IEnumerable<Guid> regionIds, CancellationToken cancellationToken = default)
    {
        var ids = Normalize(regionIds);
        var resources = await _dbContext.PageRegions.AsNoTracking()
            .Where(x => ids.Contains(x.PageRegionId))
            .Select(x => new { Id = x.PageRegionId, x.ChapterPageVersion!.ChapterPage!.Chapter!.SeriesId })
            .ToListAsync(cancellationToken);
        return await ValidateAsync(actorUserId, ids, resources.Select(x => (x.Id, x.SeriesId)), cancellationToken);
    }

    public async Task<bool> CanAccessAnnotationAsync(Guid actorUserId, Guid annotationId, CancellationToken cancellationToken = default)
    {
        var seriesIds = await _dbContext.ChapterPageAnnotations.AsNoTracking()
            .Where(x => x.ChapterPageAnnotationId == annotationId)
            .SelectMany(x => x.PageRegions)
            .Select(x => x.ChapterPageVersion!.ChapterPage!.Chapter!.SeriesId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return seriesIds.Count == 1
            && await HasSeriesAccessAsync(actorUserId, seriesIds[0], cancellationToken);
    }

    private async Task<bool> ValidateAsync(
        Guid actorUserId,
        IReadOnlyCollection<Guid> requestedIds,
        IEnumerable<(Guid Id, Guid SeriesId)> resources,
        CancellationToken cancellationToken)
    {
        if (requestedIds.Count == 0) return true;
        var resolved = resources.Distinct().ToList();
        if (resolved.Select(x => x.Id).Distinct().Count() != requestedIds.Count) return false;
        var seriesIds = resolved.Select(x => x.SeriesId).Distinct().ToList();
        return seriesIds.Count == 1 && await HasSeriesAccessAsync(actorUserId, seriesIds[0], cancellationToken);
    }

    private Task<bool> HasSeriesAccessAsync(Guid actorUserId, Guid seriesId, CancellationToken cancellationToken) =>
        _dbContext.SeriesContributors.AsNoTracking().AnyAsync(
            x => x.UserId == actorUserId && x.SeriesId == seriesId && x.EndDate == null,
            cancellationToken);

    private static Guid[] Normalize(IEnumerable<Guid> ids) =>
        ids.Distinct().ToArray();
}
