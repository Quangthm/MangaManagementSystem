using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record CancelBoardPollCommand(
    Guid PollId,
    Guid ChiefUserId)
    : IRequest<FinalizeBoardPollResultDto>;

public sealed class CancelBoardPollCommandHandler
    : IRequestHandler<CancelBoardPollCommand, FinalizeBoardPollResultDto>
{
    private readonly IEditorialBoardRepository _repository;

    public CancelBoardPollCommandHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<FinalizeBoardPollResultDto> Handle(
        CancelBoardPollCommand request,
        CancellationToken cancellationToken)
    {
        return _repository.CancelPollAsync(
            request.PollId,
            request.ChiefUserId,
            cancellationToken);
    }
}