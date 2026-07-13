using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.CompleteSeries
{
    public sealed record CompleteSeriesCommand(
        Guid SeriesId,
        Guid ActorUserId,
        string ActorRoleName)
        : IRequest<SeriesLifecycleChangedDto>;
}
