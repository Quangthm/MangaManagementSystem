using MangaManagementSystem.Application.Features.Ranking.Dtos;
using MangaManagementSystem.Application.Features.Ranking.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.Ranking.Queries;

public sealed record GetSeriesRankingQuery(
    Guid PublicationPeriodId,
    string? SearchText,
    string? Sort) : IRequest<IReadOnlyList<SeriesRankingRowDto>>;

public sealed class GetSeriesRankingQueryHandler
    : IRequestHandler<GetSeriesRankingQuery, IReadOnlyList<SeriesRankingRowDto>>
{
    private readonly ISeriesRankingRepository _repository;

    public GetSeriesRankingQueryHandler(
        ISeriesRankingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<SeriesRankingRowDto>> Handle(
        GetSeriesRankingQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetSeriesRankingAsync(
            request.PublicationPeriodId,
            request.SearchText,
            request.Sort,
            cancellationToken);
    }
}
