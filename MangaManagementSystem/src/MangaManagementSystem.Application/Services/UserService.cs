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
        private const short MinRoleId = 1;
        private const short MaxRoleId = 5;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var entity = new User
            {
                RoleId = dto.RoleId,
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                AvatarFileId = dto.AvatarFileId,
                PortfolioFileId = dto.PortfolioFileId,
                StatusCode = "PENDING_APPROVAL",
                CreatedAtUtc = DateTime.UtcNow
            };
            await _unitOfWork.Users.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
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

        public async Task<UserDto> ApproveUserAsync(int userId)
        {
            var user = await RequirePendingUserAsync(userId);

            // Approval simply activates the account. The role was chosen by the user at registration
            // and must not be changed by admins during approval.
            user.StatusCode = StatusActive;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task RejectUserAsync(int userId)
        {
            var user = await RequirePendingUserAsync(userId);

            user.StatusCode = "REJECTED";
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<User> RequirePendingUserAsync(int userId)
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

        private async Task EnsureValidRoleIdAsync(short roleId)
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
            u.Email,
            u.AvatarFileId,
            u.PortfolioFileId,
            u.StatusCode,
            u.CreatedAtUtc
        );
    }
}
