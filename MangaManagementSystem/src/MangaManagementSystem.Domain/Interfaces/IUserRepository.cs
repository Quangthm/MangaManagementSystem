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

        Task<(int newUserId, long? portfolioFileResourceId)> CreateUserWithOptionalPortfolioAsync(
            string roleName,
            string username,
            string email,
            string passwordHash,
            string? displayName = null,
            long? avatarFileId = null,
            // portfolio metadata - all nullable; pass DBNull.Value for missing
            string? portfolioOriginalFileName = null,
            string? portfolioCloudinaryPublicId = null,
            string? portfolioCloudinarySecureUrl = null,
            string? portfolioContentType = null,
            long? portfolioFileSizeBytes = null,
            string? portfolioSha256Hash = null,
            int? createdByUserId = null
        );
    }
}
