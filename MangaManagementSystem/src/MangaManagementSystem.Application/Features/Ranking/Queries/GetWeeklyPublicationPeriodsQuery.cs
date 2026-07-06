using MangaManagementSystem.Application.Features.Ranking.Dtos;
using MangaManagementSystem.Application.Features.Ranking.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.Ranking.Queries;

public sealed record GetWeeklyPublicationPeriodsQuery()
    : IRequest<IReadOnlyList<PublicationPeriodDto>>;

public sealed class GetWeeklyPublicationPeriodsQueryHandler
    : IRequestHandler<GetWeeklyPublicationPeriodsQuery, IReadOnlyList<PublicationPeriodDto>>
{
    private readonly ISeriesRankingRepository _repository;

    public GetWeeklyPublicationPeriodsQueryHandler(
        ISeriesRankingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<PublicationPeriodDto>> Handle(
        GetWeeklyPublicationPeriodsQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetWeeklyPublicationPeriodsAsync(cancellationToken);
    }
}
