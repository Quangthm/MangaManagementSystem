using MangaManagementSystem.Application.DTOs.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetUsersByStatusAsync(string status);

    Task<UserDto> ApproveUserAsync(int userId);
    Task RejectUserAsync(int userId);

    Task<UserDto> ActivateUserAsync(int userId);
    Task<UserDto> DisableUserAsync(int userId);
}
}
