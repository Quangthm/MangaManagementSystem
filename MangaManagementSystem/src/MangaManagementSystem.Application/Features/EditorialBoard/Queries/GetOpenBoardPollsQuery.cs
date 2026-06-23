using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Queries;

public sealed record GetOpenBoardPollsQuery(Guid CurrentUserId)
    : IRequest<IReadOnlyList<EditorialBoardPollDto>>;

public sealed class GetOpenBoardPollsQueryHandler
    : IRequestHandler<GetOpenBoardPollsQuery, IReadOnlyList<EditorialBoardPollDto>>
{
    private readonly IEditorialBoardRepository _repository;

    public GetOpenBoardPollsQueryHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<EditorialBoardPollDto>> Handle(
        GetOpenBoardPollsQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetOpenPollsAsync(request.CurrentUserId, cancellationToken);
    }
}