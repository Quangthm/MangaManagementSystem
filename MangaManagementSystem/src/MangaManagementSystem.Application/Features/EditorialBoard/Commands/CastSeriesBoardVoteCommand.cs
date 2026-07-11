using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record CastSeriesBoardVoteCommand(
    Guid VoterUserId,
    CastSeriesBoardVoteRequestDto Request)
    : IRequest<CastSeriesBoardVoteResultDto>;