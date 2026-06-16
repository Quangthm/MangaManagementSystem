using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> CreateUserAsync(CreateUserDto dto);

        Task<UserDto?> GetUserByIdAsync(Guid id);

        Task<UserDto?> GetUserByEmailAsync(string email);

        Task<IEnumerable<UserDto>> GetUsersByStatusAsync(string status);

        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(
            string roleName);
        Task<UserDto> ApproveUserAsync(
            Guid adminUserId,
            Guid userId);

        Task RejectUserAsync(
            Guid adminUserId,
            Guid userId,
            string? reason = null);

        Task<UserDto> ActivateUserAsync(
            Guid adminUserId,
            Guid userId);

        Task<UserDto> DisableUserAsync(
            Guid adminUserId,
            Guid userId,
            string? reason = null);

        Task<UserDto> UpdateDisplayNameAsync(
            Guid userId,
            string displayName);

        Task<UserDto> UpdateAvatarFileAsync(
            Guid userId,
            FileUploadResultDto upload);

        Task<UserDto> UpdatePortfolioFileAsync(
            Guid userId,
            FileUploadResultDto upload);

        Task ResetPasswordAsync(
            Guid userId,
            string newPassword);

        Task SendProfileOtpAsync(
            Guid userId,
            string actionCode);

        Task<bool> VerifyProfileOtpAsync(
            Guid userId,
            string actionCode,
            string otpCode);

        Task RecordProfileAuditAsync(
            Guid actorUserId,
            string actionCode,
            string detailJson);
    }
}
