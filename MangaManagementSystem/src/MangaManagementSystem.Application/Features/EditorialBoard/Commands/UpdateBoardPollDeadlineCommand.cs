using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record UpdateBoardPollDeadlineCommand(
    Guid PollId,
    Guid ChiefUserId,
    UpdateBoardPollDeadlineRequestDto Request)
    : IRequest<UpdateBoardPollDeadlineResultDto>;

public sealed class UpdateBoardPollDeadlineCommandHandler
    : IRequestHandler<UpdateBoardPollDeadlineCommand, UpdateBoardPollDeadlineResultDto>
{
    private readonly IEditorialBoardRepository _repository;

    public UpdateBoardPollDeadlineCommandHandler(
        IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<UpdateBoardPollDeadlineResultDto> Handle(
        UpdateBoardPollDeadlineCommand request,
        CancellationToken cancellationToken)
    {
        return _repository.UpdatePollDeadlineAsync(
            request.PollId,
            request.Request,
            request.ChiefUserId,
            cancellationToken);
    }
}