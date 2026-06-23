using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Contributors.Commands.EndAssistantContributor
{
    public sealed record EndAssistantContributorCommand(
        Guid ActorUserId,
        Guid SeriesId,
        Guid AssistantUserId,
        string Reason) : IRequest;
}
