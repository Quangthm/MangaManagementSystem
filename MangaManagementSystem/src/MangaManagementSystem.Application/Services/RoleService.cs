using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RoleDto?> GetRoleByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Roles.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var entities = await _unitOfWork.Roles.GetAllAsync();
            return entities.OrderBy(r => r.RoleId).Select(MapToDto);
        }

        private static RoleDto MapToDto(Role r) => new(r.RoleId, r.RoleName);
    }
}
