using MangaManagementSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<IReadOnlyList<User>> GetByStatusAsync(string status);
        Task<int> CreateUserViaProcAsync(
            string roleName,
            string username,
            string email,
            string passwordHash,
            string? displayName = null,
            long? avatarFileId = null,
            long? portfolioFileId = null,
            int? createdByUserId = null
        );
    }
}
