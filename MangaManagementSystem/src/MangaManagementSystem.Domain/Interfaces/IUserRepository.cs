using MangaManagementSystem.Domain.Entities;
using System;
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

        Task ChangeUserStatusViaProcAsync(
            Guid adminUserId,
            Guid targetUserId,
            string newStatusCode,
            string? reason = null
        );

        Task<Guid> CreateUserViaProcAsync(
            string roleName,
            string username,
            string email,
            string passwordHash,
            string? displayName = null,
            Guid? avatarFileId = null,
            Guid? portfolioFileId = null,
            Guid? createdByUserId = null
        );

        Task<(Guid newUserId, Guid? portfolioFileResourceId)> CreateUserWithOptionalPortfolioAsync(
            string roleName,
            string username,
            string email,
            string passwordHash,
            string? displayName = null,
            Guid? avatarFileId = null,
            string? portfolioOriginalFileName = null,
            string? portfolioCloudinaryPublicId = null,
            string? portfolioCloudinarySecureUrl = null,
            string? portfolioContentType = null,
            long? portfolioFileSizeBytes = null,
            string? portfolioSha256Hash = null,
            Guid? createdByUserId = null
        );

        Task UpdateDisplayNameViaProcAsync(
            Guid userId,
            string displayName
        );

        Task UpdateAvatarFileViaProcAsync(
            Guid userId,
            Guid avatarFileId
        );

        Task UpdatePortfolioFileViaProcAsync(
            Guid userId,
            Guid portfolioFileId
        );

        Task ResetPasswordViaProcAsync(
            Guid userId,
            string passwordHash
        );
    }
}