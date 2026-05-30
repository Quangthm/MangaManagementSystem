using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var entity = new User
            {
                RoleId = dto.RoleId,
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = dto.Password, // TODO: Hash password in future
                AvatarFileId = dto.AvatarFileId,
                PortfolioFileId = dto.PortfolioFileId,
                Status = "PENDING_APPROVAL",
                CreatedAt = System.DateTime.UtcNow
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

        private static UserDto MapToDto(User u) => new(
            u.UserId,
            u.RoleId,
            u.Username,
            u.Email,
            u.AvatarFileId,
            u.PortfolioFileId,
            u.Status,
            u.CreatedAt
        );
    }
}
