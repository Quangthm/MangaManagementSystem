using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Contributors.Commands.AddAssistantContributor
{
    public sealed class AddAssistantContributorCommandHandler
        : IRequestHandler<AddAssistantContributorCommand>
    {
        private readonly ISeriesContributorManagementRepository _seriesContributorRepository;

        public AddAssistantContributorCommandHandler(ISeriesContributorManagementRepository seriesContributorRepository)
        {
            _seriesContributorRepository = seriesContributorRepository;
        }

        public async Task Handle(
            AddAssistantContributorCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid signed-in user is required to add a contributor.");
            }

            if (request.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid series must be selected to add a contributor.");
            }

            if (request.AssistantUserId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid assistant user must be selected.");
            }

            bool actorIsMangaka = await _seriesContributorRepository.IsActiveMangakaContributorAsync(
                request.ActorUserId,
                request.SeriesId,
                cancellationToken);

            if (!actorIsMangaka)
            {
                throw new InvalidOperationException("Only an active Mangaka contributor can add assistants to this series.");
            }

            var target = await _seriesContributorRepository.GetContributorTargetSnapshotAsync(
                request.SeriesId,
                request.AssistantUserId,
                cancellationToken);

            if (!target.Exists)
            {
                throw new InvalidOperationException("The selected assistant user does not exist.");
            }

            if (!string.Equals(target.StatusCode, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The selected assistant user is not currently active.");
            }

            if (!string.Equals(target.RoleName, "Assistant", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only users with the Assistant role can be added as assistant contributors.");
            }

            if (target.IsActiveContributor)
            {
                throw new InvalidOperationException("This assistant is already an active contributor of the selected series.");
            }

            await _seriesContributorRepository.AddContributorViaProcAsync(
                request.ActorUserId,
                request.SeriesId,
                request.AssistantUserId,
                notes: null,
                cancellationToken);
        }
    }
}
