using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Domain.Entities;


namespace MangaManagementSystem.Application.Mappers
{
    public static class UserMapper
    {
        public static UserDto ToDto(this User user)
        {
            var roleName = user.Role?.RoleName;

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new InvalidOperationException(
                    "User.Role was not loaded. Use a repository method that includes Role.");
            }

            return new UserDto(
                user.UserId,
                user.Username,
                user.DisplayName,
                user.Email,
                user.AvatarFileId,
                user.PortfolioFileId,
                user.StatusCode,
                user.CreatedAtUtc,
                roleName
            );
        }
    }
}
