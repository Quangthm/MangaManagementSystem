using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record OpenSeriesBoardPollCommand(
    Guid ChiefUserId,
    OpenSeriesBoardPollRequestDto Request)
    : IRequest<OpenSeriesBoardPollResultDto>;

public sealed class OpenSeriesBoardPollCommandHandler
    : IRequestHandler<OpenSeriesBoardPollCommand, OpenSeriesBoardPollResultDto>
{
    private readonly IEditorialBoardRepository _repository;

    public OpenSeriesBoardPollCommandHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<OpenSeriesBoardPollResultDto> Handle(
        OpenSeriesBoardPollCommand request,
        CancellationToken cancellationToken)
    {
        return _repository.OpenPollAsync(
            request.Request,
            request.ChiefUserId,
            cancellationToken);
    }
}