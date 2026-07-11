using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record OpenSeriesBoardPollCommand(
    Guid ChiefUserId,
    OpenSeriesBoardPollRequestDto Request)
    : IRequest<OpenSeriesBoardPollResultDto>;