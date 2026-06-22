using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.Series.Queries.GetEditorSeries
{
    public sealed record GetEditorSeriesQuery(Guid ActorUserId) : IRequest<EditorSeriesListDto>;
}
