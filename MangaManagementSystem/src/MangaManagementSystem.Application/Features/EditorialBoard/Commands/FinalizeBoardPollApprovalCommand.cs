using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record FinalizeBoardPollApprovalCommand(
    Guid PollId,
    Guid ChiefUserId)
    : IRequest<FinalizeBoardPollResultDto>;