using SeriesEntity = MangaManagementSystem.Domain.Entities.Series;

namespace MangaManagementSystem.Application.Features.Editor.Series.Ports;

public interface IEditorSeriesRepository
{
    Task<IReadOnlyList<SeriesEntity>> GetSeriesAsync(
        Guid actorUserId,
        CancellationToken ct = default);
}
