using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.PublicationFrequencyRequests
{
    public sealed record RequestPublicationFrequencyChangeCommand(
        Guid ActorUserId,
        Guid SeriesId,
        string Reason)
        : IRequest<PublicationFrequencyChangeRequestResultDto>;
}
