using MangaManagementSystem.Application.Features.Ranking.Dtos;
using MangaManagementSystem.Application.Features.Ranking.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.Ranking.Queries;

public sealed record SearchRankableSeriesQuery(
    Guid PublicationPeriodId,
    string? SearchText,
    int MaxResults) : IRequest<IReadOnlyList<RankableSeriesDto>>;

public sealed class SearchRankableSeriesQueryHandler
    : IRequestHandler<SearchRankableSeriesQuery, IReadOnlyList<RankableSeriesDto>>
{
    private readonly ISeriesRankingRepository _repository;

    public SearchRankableSeriesQueryHandler(
        ISeriesRankingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<RankableSeriesDto>> Handle(
        SearchRankableSeriesQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.SearchRankableSeriesAsync(
            request.PublicationPeriodId,
            request.SearchText,
            request.MaxResults <= 0 ? 10 : request.MaxResults,
            cancellationToken);
    }
}
