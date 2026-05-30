using MangaManagementSystem.Application.DTOs.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IRoleService
    {
        Task<RoleDto?> GetRoleByIdAsync(short id);
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    }
}
