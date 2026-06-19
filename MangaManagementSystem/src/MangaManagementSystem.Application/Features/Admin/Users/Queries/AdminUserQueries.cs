using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Users.Queries
{
    public sealed record GetAdminUsersQuery(
        string? StatusCode = null)
        : IRequest<IReadOnlyList<UserDto>>;

    public sealed record SearchAdminUsersQuery(
        string? Search,
        string? StatusCode,
        string? RoleName,
        int PageNumber = 1,
        int PageSize = 20)
        : IRequest<AdminUserPageDto>;

    public sealed record GetAdminUserDetailQuery(
        Guid UserId)
        : IRequest<AdminUserDetailDto?>;

    public sealed record GetAdminUserAuditQuery(
        Guid UserId,
        int PageNumber = 1,
        int PageSize = 20)
        : IRequest<AdminUserAuditPageDto>;

    public sealed record GetAdminUserStatusCountsQuery
        : IRequest<AdminUserStatusCountsDto>
    {
    }

    public sealed record GetAdminUserPortfolioQuery(
        Guid UserId)
        : IRequest<FileResourceDto?>;
}
