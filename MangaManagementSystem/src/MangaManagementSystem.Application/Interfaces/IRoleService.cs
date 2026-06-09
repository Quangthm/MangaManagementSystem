using MangaManagementSystem.Application.DTOs.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IRoleService
    {
        Task<RoleDto?> GetRoleByIdAsync(Guid id);
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    }
}
