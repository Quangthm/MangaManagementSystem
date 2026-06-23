using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Contributors.Queries.GetSeriesContributors
{
    public sealed class GetSeriesContributorsQueryHandler
        : IRequestHandler<GetSeriesContributorsQuery, IReadOnlyList<SeriesContributorListItemDto>>
    {
        private readonly ISeriesContributorManagementRepository _seriesContributorRepository;

        public GetSeriesContributorsQueryHandler(ISeriesContributorManagementRepository seriesContributorRepository)
        {
            _seriesContributorRepository = seriesContributorRepository;
        }

        public async Task<IReadOnlyList<SeriesContributorListItemDto>> Handle(
            GetSeriesContributorsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid signed-in user is required to view contributors.");
            }

            if (request.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid series must be selected to view contributors.");
            }

            bool canAccess = await _seriesContributorRepository.IsActiveMangakaContributorAsync(
                request.ActorUserId,
                request.SeriesId,
                cancellationToken);

            if (!canAccess)
            {
                throw new InvalidOperationException("You can only view contributors for series where you are an active Mangaka contributor.");
            }

            return await _seriesContributorRepository.GetSeriesContributorsAsync(
                request.ActorUserId,
                request.SeriesId,
                cancellationToken);
        }
    }
}
