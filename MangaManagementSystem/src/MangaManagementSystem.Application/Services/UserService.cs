using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class UserService : IUserService
    {
        private const string StatusPendingApproval = "PENDING_APPROVAL";
        private const string StatusActive = "ACTIVE";
        private const string StatusDisabled = "DISABLED";

        private static readonly Guid MinRoleId = new Guid("00000000-0000-0000-0000-000000000001");
        private static readonly Guid MaxRoleId = new Guid("00000000-0000-0000-0000-000000000005");

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId);
            var roleCode = role?.RoleName ?? string.Empty;
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            var displayName = string.IsNullOrWhiteSpace(dto.DisplayName)
                ? dto.Username
                : dto.DisplayName.Trim();

            var newUserId = await _unitOfWork.Users.CreateUserViaProcAsync(
                roleCode,
                dto.Username,
                dto.Email,
                passwordHash,
                displayName,
                dto.AvatarFileId,
                dto.PortfolioFileId,
                null);

            var created = await _unitOfWork.Users.GetByIdAsync(newUserId);

            if (created == null)
            {
                throw new InvalidOperationException("Created user could not be loaded.");
            }

            return MapToDto(created);
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Users.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var entity = await _unitOfWork.Users.GetByEmailAsync(email);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<UserDto>> GetUsersByStatusAsync(string status)
        {
            var entities = await _unitOfWork.Users.GetByStatusAsync(status);
            return entities.Select(MapToDto);
        }

        public async Task<UserDto> ApproveUserAsync(Guid adminUserId, Guid userId)
        {
            await RequirePendingUserAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusActive,
                "User registration approved.");

            var updated = await _unitOfWork.Users.GetByIdAsync(userId);

            if (updated == null)
            {
                throw new InvalidOperationException($"User {userId} was not found after approval.");
            }

            return MapToDto(updated);
        }

        public async Task RejectUserAsync(Guid adminUserId, Guid userId, string? reason = null)
        {
            await RequirePendingUserAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                "REJECTED",
                reason ?? "User registration rejected.");
        }

        public async Task<UserDto> ActivateUserAsync(Guid adminUserId, Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusActive,
                "User account activated.");

            var updated = await _unitOfWork.Users.GetByIdAsync(userId);

            if (updated == null)
            {
                throw new InvalidOperationException($"User {userId} was not found after activation.");
            }

            return MapToDto(updated);
        }

        public async Task<UserDto> DisableUserAsync(Guid adminUserId, Guid userId, string? reason = null)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusDisabled,
                reason ?? "User account disabled.");

            var updated = await _unitOfWork.Users.GetByIdAsync(userId);

            if (updated == null)
            {
                throw new InvalidOperationException($"User {userId} was not found after disabling.");
            }

            return MapToDto(updated);
        }

        public async Task<UserDto> UpdateDisplayNameAsync(Guid userId, string displayName)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            var trimmedDisplayName = displayName?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedDisplayName))
            {
                throw new InvalidOperationException("Display name cannot be empty.");
            }

            await _unitOfWork.Users.UpdateDisplayNameViaProcAsync(
                userId,
                trimmedDisplayName);

            var updated = await _unitOfWork.Users.GetByIdAsync(userId);

            if (updated == null)
            {
                throw new InvalidOperationException($"User {userId} was not found after display name update.");
            }

            return MapToDto(updated);
        }

        public async Task<UserDto> UpdateAvatarFileAsync(Guid userId, Guid avatarFileId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            if (avatarFileId == Guid.Empty)
            {
                throw new InvalidOperationException("Avatar file id is invalid.");
            }

            await _unitOfWork.Users.UpdateAvatarFileViaProcAsync(
                userId,
                avatarFileId);

            var updated = await _unitOfWork.Users.GetByIdAsync(userId);

            if (updated == null)
            {
                throw new InvalidOperationException($"User {userId} was not found after avatar update.");
            }

            return MapToDto(updated);
        }

        public async Task<UserDto> UpdatePortfolioFileAsync(Guid userId, Guid portfolioFileId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            if (portfolioFileId == Guid.Empty)
            {
                throw new InvalidOperationException("Portfolio file id is invalid.");
            }

            await _unitOfWork.Users.UpdatePortfolioFileViaProcAsync(
                userId,
                portfolioFileId);

            var updated = await _unitOfWork.Users.GetByIdAsync(userId);

            if (updated == null)
            {
                throw new InvalidOperationException($"User {userId} was not found after portfolio update.");
            }

            return MapToDto(updated);
        }

        public async Task ResetPasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                throw new InvalidOperationException("Current password is required.");
            }

            if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new InvalidOperationException("New password cannot be empty.");
            }

            if (newPassword.Length < 8)
            {
                throw new InvalidOperationException("New password must be at least 8 characters.");
            }

            if (_passwordHasher.VerifyPassword(newPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("New password must be different from the current password.");
            }

            var passwordHash = _passwordHasher.HashPassword(newPassword);

            await _unitOfWork.Users.ResetPasswordViaProcAsync(
                userId,
                passwordHash);
        }

        private async Task<User> RequirePendingUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            if (user.StatusCode != StatusPendingApproval)
            {
                throw new InvalidOperationException(
                    $"User {userId} cannot be processed because their status is '{user.StatusCode}', not '{StatusPendingApproval}'.");
            }

            return user;
        }

        private async Task EnsureValidRoleIdAsync(Guid roleId)
        {
            if (roleId < MinRoleId || roleId > MaxRoleId)
            {
                throw new InvalidOperationException(
                    $"Role id {roleId} is invalid. Allowed roles are {MinRoleId} through {MaxRoleId}.");
            }

            if (await _unitOfWork.Roles.GetByIdAsync(roleId) == null)
            {
                throw new InvalidOperationException($"Role id {roleId} does not exist.");
            }
        }

        private static UserDto MapToDto(User u) => new(
            u.UserId,
            u.RoleId,
            u.Username,
            u.DisplayName,
            u.Email,
            u.AvatarFileId,
            u.PortfolioFileId,
            u.StatusCode,
            u.CreatedAtUtc,
            u.Role?.RoleName
        );
    }
}