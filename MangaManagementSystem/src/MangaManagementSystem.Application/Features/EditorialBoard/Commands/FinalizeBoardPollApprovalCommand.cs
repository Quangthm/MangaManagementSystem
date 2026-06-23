using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record FinalizeBoardPollApprovalCommand(
    Guid PollId,
    Guid ChiefUserId)
    : IRequest<FinalizeBoardPollResultDto>;

public sealed class FinalizeBoardPollApprovalCommandHandler
    : IRequestHandler<FinalizeBoardPollApprovalCommand, FinalizeBoardPollResultDto>
{
    private readonly IEditorialBoardRepository _repository;

    public FinalizeBoardPollApprovalCommandHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<FinalizeBoardPollResultDto> Handle(
        FinalizeBoardPollApprovalCommand request,
        CancellationToken cancellationToken)
    {
        return _repository.FinalizeApprovalAsync(
            request.PollId,
            request.ChiefUserId,
            cancellationToken);
    }
}