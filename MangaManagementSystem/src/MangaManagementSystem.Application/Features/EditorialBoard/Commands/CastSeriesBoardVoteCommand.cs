using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record CastSeriesBoardVoteCommand(
    Guid VoterUserId,
    CastSeriesBoardVoteRequestDto Request)
    : IRequest<CastSeriesBoardVoteResultDto>;

public sealed class CastSeriesBoardVoteCommandHandler
    : IRequestHandler<CastSeriesBoardVoteCommand, CastSeriesBoardVoteResultDto>
{
    private readonly IEditorialBoardRepository _repository;

    public CastSeriesBoardVoteCommandHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<CastSeriesBoardVoteResultDto> Handle(
        CastSeriesBoardVoteCommand request,
        CancellationToken cancellationToken)
    {
        return _repository.CastVoteAsync(
            request.Request,
            request.VoterUserId,
            cancellationToken);
    }
}