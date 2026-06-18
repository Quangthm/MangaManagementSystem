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
    }
}
