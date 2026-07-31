using MangaManagementSystem.Application.Features.Editor.Dashboard.Models;

namespace MangaManagementSystem.Application.Features.Editor.Dashboard.Ports;

/// <summary>
/// Read-only repository for the Tantou Editor dashboard. All queries are EF
/// <c>AsNoTracking</c> read models; no writes or stored-procedure transitions live here.
/// Returns Domain entities and primitive counts only — DTO shaping happens in the
/// Application handler so the Domain layer stays free of Application dependencies.
/// </summary>
public interface IEditorDashboardRepository
{
    Task<EditorDashboardData> GetDashboardDataAsync(
        Guid actorUserId,
        int proposalQueueTake,
        int recentSeriesTake,
        CancellationToken ct = default);
}
