using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Repositories;

public interface IEditorialBoardRepository
{
    Task<EditorialDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
}