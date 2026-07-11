using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Queries;

public sealed record GetBoardPollHistoryQuery(
    Guid CurrentUserId)
    : IRequest<IReadOnlyList<EditorialBoardPollDto>>;