using MangaManagementSystem.Application.Features.Ranking.Dtos;
using MangaManagementSystem.Application.Features.Ranking.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.Ranking.Queries;

public sealed record GetSeriesVoteInputsQuery(
    Guid PublicationPeriodId,
    string? SearchText,
    string? Sort) : IRequest<IReadOnlyList<SeriesVoteInputDto>>;

public sealed class GetSeriesVoteInputsQueryHandler
    : IRequestHandler<GetSeriesVoteInputsQuery, IReadOnlyList<SeriesVoteInputDto>>
{
    private readonly ISeriesRankingRepository _repository;

    public GetSeriesVoteInputsQueryHandler(
        ISeriesRankingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<SeriesVoteInputDto>> Handle(
        GetSeriesVoteInputsQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetSeriesVoteInputsAsync(
            request.PublicationPeriodId,
            request.SearchText,
            request.Sort,
            cancellationToken);
    }
}
