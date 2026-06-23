using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Assistant.CompletedWork.Queries.GetAssistantCompletedWork
{
    public sealed record GetAssistantCompletedWorkQuery(
        Guid ActorUserId
    ) : IRequest<AssistantCompletedWorkSummaryDto>;
}
