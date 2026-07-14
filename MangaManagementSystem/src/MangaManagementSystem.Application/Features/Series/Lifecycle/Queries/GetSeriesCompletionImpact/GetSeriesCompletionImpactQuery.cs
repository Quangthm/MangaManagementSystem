using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Queries.GetSeriesCompletionImpact
{
    public sealed record GetSeriesCompletionImpactQuery(
        Guid SeriesId,
        Guid ActorUserId,
        string ActorRoleName)
        : IRequest<SeriesCompletionImpactDto>;
}
