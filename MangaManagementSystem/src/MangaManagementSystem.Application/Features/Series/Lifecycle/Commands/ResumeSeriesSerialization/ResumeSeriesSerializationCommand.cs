using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.ResumeSeriesSerialization
{
    public sealed record ResumeSeriesSerializationCommand(
        Guid SeriesId,
        Guid ActorUserId,
        string ActorRoleName)
        : IRequest<SeriesLifecycleChangedDto>;
}
