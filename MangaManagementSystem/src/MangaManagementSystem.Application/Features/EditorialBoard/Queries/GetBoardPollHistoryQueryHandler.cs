using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Queries;

public sealed class GetBoardPollHistoryQueryHandler
    : IRequestHandler<GetBoardPollHistoryQuery, IReadOnlyList<EditorialBoardPollDto>>
{
    private readonly IEditorialBoardRepository _repository;

    public GetBoardPollHistoryQueryHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<EditorialBoardPollDto>> Handle(
        GetBoardPollHistoryQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetPollHistoryAsync(
            request.CurrentUserId,
            cancellationToken);
    }
}
