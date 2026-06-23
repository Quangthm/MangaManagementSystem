using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Contributors.Commands.AddAssistantContributor
{
    public sealed record AddAssistantContributorCommand(
        Guid ActorUserId,
        Guid SeriesId,
        Guid AssistantUserId) : IRequest;
}
