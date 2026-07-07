using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IAdminUserApiClient
    {
        Task<IReadOnlyList<UserDto>> GetUsersAsync(
            string? statusCode = null,
            CancellationToken cancellationToken = default);

        Task<AdminUserPageDto> SearchUsersAsync(
            string? search = null,
            string? statusCode = null,
            string? roleName = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<AdminUserDetailDto?> GetUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<AdminUserAuditPageDto> GetUserAuditAsync(
            Guid userId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<AdminUserStatusCountsDto>
            GetStatusCountsAsync(
                CancellationToken cancellationToken = default);

        Task<FileResourceDto?> GetPortfolioAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<UserDto> ApproveUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<UserDto> RejectUserAsync(
            Guid userId,
            string? reason = null,
            CancellationToken cancellationToken = default);

        Task<UserDto> DisableUserAsync(
            Guid userId,
            string? reason = null,
            CancellationToken cancellationToken = default);

        Task<UserDto> ActivateUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(
            Guid userId,
            string resetPageUrl,
            CancellationToken cancellationToken = default);
    }
}
