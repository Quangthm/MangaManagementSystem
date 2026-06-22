using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.Dashboard.Queries.GetEditorDashboard
{
    /// <summary>
    /// Read-only query that builds the Tantou Editor dashboard read model
    /// (KPI counts + proposal queue preview + recent series activity).
    /// </summary>
    public sealed record GetEditorDashboardQuery(Guid ActorUserId) : IRequest<EditorDashboardDto>;
}
