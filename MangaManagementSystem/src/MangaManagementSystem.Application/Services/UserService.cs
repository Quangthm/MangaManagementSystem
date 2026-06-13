
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Mappers;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;


namespace MangaManagementSystem.Application.Services
{
    public class UserService : IUserService
    {
        private const string StatusPendingApproval = "PENDING_APPROVAL";
        private const string StatusActive = "ACTIVE";
        private const string StatusDisabled = "DISABLED";
        private const string StatusRejected = "REJECTED";

        private static readonly HashSet<string> AllowedRoleNames = new(StringComparer.OrdinalIgnoreCase)
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
            var roleName = NormalizeRoleName(dto.RoleName);
            EnsureValidRoleName(roleName);

            var username = dto.Username.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            var newUserId = await _unitOfWork.Users.CreateUserViaProcAsync(
                roleName,
                username,
                email,
                passwordHash,
                dto.DisplayName,
                dto.AvatarFileId,
                dto.PortfolioFileId,
                null);

            var created = await GetRequiredUserByIdForDtoAsync(newUserId);
            return created.ToDto();
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var entity = await GetUserByIdForDtoAsync(id);
            return entity is null ? null : entity.ToDto();
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var entity = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);

            return entity is null ? null : entity.ToDto();
        }

        public async Task<IEnumerable<UserDto>> GetUsersByStatusAsync(string status)
        {
            var entities = await _unitOfWork.Users.GetByStatusAsync(status);
            return entities.Select(user => user.ToDto());
        }

        public async Task<UserDto> ApproveUserAsync(Guid adminUserId, Guid userId)
        {
            await RequirePendingUserAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusActive,
                "User registration approved.");

            var updated = await GetRequiredUserByIdForDtoAsync(userId);
            return updated.ToDto();
        }

        public async Task RejectUserAsync(Guid adminUserId, Guid userId, string? reason = null)
        {
            await RequirePendingUserAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusRejected,
                string.IsNullOrWhiteSpace(reason)
                    ? "User registration rejected."
                    : reason.Trim());
        }

        public async Task<UserDto> ActivateUserAsync(Guid adminUserId, Guid userId)
        {
            await GetRequiredUserByIdAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusActive,
                "User account activated.");

            var updated = await GetRequiredUserByIdForDtoAsync(userId);
            return updated.ToDto();
        }

        public async Task<UserDto> DisableUserAsync(Guid adminUserId, Guid userId, string? reason = null)
        {
            await GetRequiredUserByIdAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusDisabled,
                string.IsNullOrWhiteSpace(reason)
                    ? "User account disabled."
                    : reason.Trim());

            var updated = await GetRequiredUserByIdForDtoAsync(userId);
            return updated.ToDto();
        }

        private async Task<User> RequirePendingUserAsync(Guid userId)
        {
            var user = await GetRequiredUserByIdAsync(userId);

            if (!string.Equals(user.StatusCode, StatusPendingApproval, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"User {userId} cannot be processed because their status is '{user.StatusCode}', not '{StatusPendingApproval}'.");
            }

            return user;
        }

        private async Task<User> GetRequiredUserByIdAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user is null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            return user;
        }

        private async Task<User?> GetUserByIdForDtoAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user is null)
            {
                return null;
            }

            if (user.Role is not null)
            {
                return user;
            }

            /*
                Generic GetByIdAsync usually does not Include Role.
                Current UserRepository.GetByEmailAsync does Include Role, so we reload through email
                before mapping to UserDto.
            */
            var userWithRole = await _unitOfWork.Users.GetByEmailAsync(user.Email);
            return userWithRole ?? user;
        }

        private async Task<User> GetRequiredUserByIdForDtoAsync(Guid userId)
        {
            var user = await GetUserByIdForDtoAsync(userId);

            if (user is null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
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

        private static string NormalizeRoleName(string roleName)
            => roleName.Trim();
    }
}

