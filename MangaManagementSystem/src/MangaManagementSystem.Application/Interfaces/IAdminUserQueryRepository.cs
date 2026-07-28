using MangaManagementSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public sealed record UserSearchCriteria(
        string? Search,
        string? StatusCode,
        string? RoleName,
        int PageNumber,
        int PageSize);

    public sealed record PagedUserResult(
        IReadOnlyList<User> Items,
        int TotalCount);

    public interface IAdminUserQueryRepository
    {
        Task<PagedUserResult> SearchAdminUsersAsync(
            UserSearchCriteria criteria,
            CancellationToken cancellationToken = default);
    }
}