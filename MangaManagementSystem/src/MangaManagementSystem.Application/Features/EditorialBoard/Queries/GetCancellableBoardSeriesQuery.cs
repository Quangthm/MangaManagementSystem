using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Queries;

public sealed record GetCancellableBoardSeriesQuery()
    : IRequest<IReadOnlyList<CancellableBoardSeriesDto>>;

public sealed class GetCancellableBoardSeriesQueryHandler
    : IRequestHandler<GetCancellableBoardSeriesQuery, IReadOnlyList<CancellableBoardSeriesDto>>
{
    private readonly IEditorialBoardRepository _repository;

    public GetCancellableBoardSeriesQueryHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<CancellableBoardSeriesDto>> Handle(
        GetCancellableBoardSeriesQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetCancellableSeriesForCancelPollAsync(cancellationToken);
    }
}