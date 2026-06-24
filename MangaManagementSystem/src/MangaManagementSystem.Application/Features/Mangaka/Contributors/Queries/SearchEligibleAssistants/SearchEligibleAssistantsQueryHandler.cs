using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Contributors.Queries.SearchEligibleAssistants
{
    public sealed class SearchEligibleAssistantsQueryHandler
        : IRequestHandler<SearchEligibleAssistantsQuery, IReadOnlyList<EligibleAssistantContributorDto>>
    {
        private readonly ISeriesContributorManagementRepository _seriesContributorRepository;

        public SearchEligibleAssistantsQueryHandler(ISeriesContributorManagementRepository seriesContributorRepository)
        {
            _seriesContributorRepository = seriesContributorRepository;
        }

        public async Task<IReadOnlyList<EligibleAssistantContributorDto>> Handle(
            SearchEligibleAssistantsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid signed-in user is required to search eligible assistants.");
            }

            if (request.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid series must be selected to search eligible assistants.");
            }

            bool canAccess = await _seriesContributorRepository.IsActiveMangakaContributorAsync(
                request.ActorUserId,
                request.SeriesId,
                cancellationToken);

            if (!canAccess)
            {
                throw new InvalidOperationException("You can only manage contributors for series where you are an active Mangaka contributor.");
            }

            return await _seriesContributorRepository.SearchEligibleAssistantContributorsAsync(
                request.ActorUserId,
                request.SeriesId,
                request.Search,
                cancellationToken);
        }
    }
}
