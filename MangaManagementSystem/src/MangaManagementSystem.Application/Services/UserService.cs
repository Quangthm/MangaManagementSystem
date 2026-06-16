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

        private static readonly HashSet<string> AllowedRoleNames = new(StringComparer.Ordinal)
        {
            "Admin",
            "Mangaka",
            "Assistant",
            "Tantou Editor",
            "Editorial Board Member",
            "Editorial Board Chief"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            EnsureValidRoleName(dto.RoleName);
            var passwordHash = _passwordHasher.HashPassword(dto.Password);
            var newUserId = await _unitOfWork.Users.CreateUserViaProcAsync(
                dto.RoleName,
                dto.Username,
                dto.Email,
                passwordHash,
                dto.DisplayName,
                dto.AvatarFileId,
                dto.PortfolioFileId,
                null);

            var created = await _unitOfWork.Users.GetByIdAsync(newUserId);
            return MapToDto(created!);
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
            return MapToDto(updated!);
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

        private static void EnsureValidRoleName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName) || !AllowedRoleNames.Contains(roleName))
            {
                throw new InvalidOperationException(
                    $"Role '{roleName}' is invalid. Allowed roles are: {string.Join(", ", AllowedRoleNames)}.");
            }
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
            return MapToDto(updated!);
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
            return MapToDto(updated!);
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
