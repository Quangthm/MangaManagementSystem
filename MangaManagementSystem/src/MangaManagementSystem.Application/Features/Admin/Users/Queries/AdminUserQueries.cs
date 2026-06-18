using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Users.Queries
{
    public sealed record GetAdminUsersQuery(
        string? StatusCode = null)
        : IRequest<IReadOnlyList<UserDto>>;

    public sealed record GetAdminUserStatusCountsQuery
        : IRequest<AdminUserStatusCountsDto>
    {
    }

    public sealed record GetAdminUserPortfolioQuery(
        Guid UserId)
        : IRequest<FileResourceDto?>;
}
