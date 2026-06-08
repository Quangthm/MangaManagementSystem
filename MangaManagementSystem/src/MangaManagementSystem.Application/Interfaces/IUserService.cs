using MangaManagementSystem.Application.DTOs.Auth;
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

    Task<UserDto> ApproveUserAsync(Guid adminUserId, Guid userId);
    Task RejectUserAsync(Guid adminUserId, Guid userId, string? reason = null);

    Task<UserDto> ActivateUserAsync(Guid adminUserId, Guid userId);
    Task<UserDto> DisableUserAsync(Guid adminUserId, Guid userId, string? reason = null);
}
}
