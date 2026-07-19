using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record OpenCancelSerializationPollCommand(
    Guid SeriesId,
    Guid ChiefUserId,
    OpenCancelSerializationPollRequestDto Request)
    : IRequest<OpenSeriesBoardPollResultDto>;

public sealed class OpenCancelSerializationPollCommandHandler
    : IRequestHandler<OpenCancelSerializationPollCommand, OpenSeriesBoardPollResultDto>
{
    private readonly IEditorialBoardRepository _repository;

    public OpenCancelSerializationPollCommandHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<OpenSeriesBoardPollResultDto> Handle(
        OpenCancelSerializationPollCommand request,
        CancellationToken cancellationToken)
    {
        return _repository.OpenCancelSerializationPollAsync(
            request.SeriesId,
            request.Request,
            request.ChiefUserId,
            cancellationToken);
    }
}