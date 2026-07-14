using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.SetSeriesHiatus
{
    public sealed record SetSeriesHiatusCommand(
        Guid SeriesId,
        Guid ActorUserId,
        string ActorRoleName,
        string Reason)
        : IRequest<SeriesLifecycleChangedDto>;
}
