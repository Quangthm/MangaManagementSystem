using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Queries;

public sealed record GetEditorialDashboardQuery()
    : IRequest<EditorialDashboardDto>;

public sealed class GetEditorialDashboardQueryHandler
    : IRequestHandler<GetEditorialDashboardQuery, EditorialDashboardDto>
{
    private readonly IEditorialBoardRepository _repository;

    public GetEditorialDashboardQueryHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<EditorialDashboardDto> Handle(
        GetEditorialDashboardQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetDashboardAsync(cancellationToken);
    }
}