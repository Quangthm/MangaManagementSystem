using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Queries;

public sealed record GetEditorialDashboardQuery()
    : IRequest<EditorialDashboardDto>;