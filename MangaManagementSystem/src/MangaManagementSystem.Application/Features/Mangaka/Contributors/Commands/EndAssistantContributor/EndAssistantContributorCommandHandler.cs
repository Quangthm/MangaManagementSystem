using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Contributors.Commands.EndAssistantContributor
{
    public sealed class EndAssistantContributorCommandHandler
        : IRequestHandler<EndAssistantContributorCommand>
    {
        private const int MaxReasonLength = 500;

        private readonly ISeriesContributorManagementRepository _seriesContributorRepository;

        public EndAssistantContributorCommandHandler(ISeriesContributorManagementRepository seriesContributorRepository)
        {
            _seriesContributorRepository = seriesContributorRepository;
        }

        public async Task Handle(
            EndAssistantContributorCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid signed-in user is required to remove a contributor.");
            }

            if (request.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid series must be selected to remove a contributor.");
            }

            if (request.AssistantUserId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid assistant user must be selected.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("A reason is required to remove an assistant contributor.");
            }

            if (request.Reason.Length > MaxReasonLength)
            {
                throw new InvalidOperationException($"The reason must be {MaxReasonLength} characters or fewer.");
            }

            bool actorIsMangaka = await _seriesContributorRepository.IsActiveMangakaContributorAsync(
                request.ActorUserId,
                request.SeriesId,
                cancellationToken);

            if (!actorIsMangaka)
            {
                throw new InvalidOperationException("Only an active Mangaka contributor can remove assistants from this series.");
            }

            var target = await _seriesContributorRepository.GetContributorTargetSnapshotAsync(
                request.SeriesId,
                request.AssistantUserId,
                cancellationToken);

            if (!target.Exists)
            {
                throw new InvalidOperationException("The selected assistant user does not exist.");
            }

            if (!string.Equals(target.RoleName, "Assistant", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only assistant contributors can be removed in this workflow.");
            }

            if (!target.IsActiveContributor)
            {
                throw new InvalidOperationException("This contributor is not currently active on the selected series.");
            }

            bool hasActiveTasks = await _seriesContributorRepository.HasActiveTasksForSeriesAsync(
                request.AssistantUserId,
                request.SeriesId,
                cancellationToken);

            if (hasActiveTasks)
            {
                throw new InvalidOperationException(
                    "This assistant has active tasks. Reassign or cancel their tasks before removing them from the series.");
            }

            await _seriesContributorRepository.EndAssistantContributorViaProcAsync(
                request.ActorUserId,
                request.SeriesId,
                request.AssistantUserId,
                request.Reason.Trim(),
                cancellationToken);
        }
    }
}
