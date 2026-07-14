using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Queries.GetSeriesLifecycleActions
{
    public sealed record GetSeriesLifecycleActionsQuery(
        Guid SeriesId,
        Guid ActorUserId,
        string ActorRoleName)
        : IRequest<SeriesLifecycleActionsDto>;
}
