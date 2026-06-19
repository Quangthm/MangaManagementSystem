using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
    public sealed record UserFileReplacementRequest(
        Guid UserId,
        string OriginalFileName,
        string CloudinaryPublicId,
        string CloudinarySecureUrl,
        string ContentType,
        long FileSizeBytes,
        string Sha256Hash
    );

    public sealed record UserFileReplacementResult(
        Guid NewFileResourceId,
        Guid? OldFileResourceId,
        string? OldCloudinaryPublicId,
        string? OldContentType
    );

    public sealed record UserSearchCriteria(
        string? Search,
        string? StatusCode,
        string? RoleName,
        int PageNumber,
        int PageSize);

    public sealed record PagedUserResult(
        IReadOnlyList<User> Items,
        int TotalCount);

    public interface IUserRepository
        : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(
            string email);

        Task<User?> GetByUsernameAsync(
            string username);

        Task<User?> GetByUsernameOrEmailAsync(
            string usernameOrEmail);

        Task<User?> GetByIdWithRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> GetAllWithRoleAsync(
            CancellationToken cancellationToken = default);

        Task<PagedUserResult> SearchAdminUsersAsync(
            UserSearchCriteria criteria,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, int>>
            GetStatusCountsAsync(
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> GetByStatusAsync(
            string status);

        Task<IReadOnlyList<User>> GetByRoleNameAsync(
            string roleName);

        Task<User?> GetByPortfolioFileIdAsync(
            Guid portfolioFileId);

        Task ChangeUserStatusViaProcAsync(
            Guid adminUserId,
            Guid targetUserId,
            string newStatusCode,
            string? reason = null);

        Task<Guid> CreateUserViaProcAsync(
            string roleName,
            string username,
            string email,
            string passwordHash,
            string? displayName = null,
            Guid? avatarFileId = null,
            Guid? portfolioFileId = null,
            Guid? createdByUserId = null);

        Task<(
            Guid newUserId,
            Guid? portfolioFileResourceId)>
            CreateUserWithOptionalPortfolioAsync(
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
                Guid? createdByUserId = null);

        Task UpdateDisplayNameViaProcAsync(
            Guid userId,
            string displayName);

        Task<UserFileReplacementResult>
            UpdateAvatarFileViaProcAsync(
                UserFileReplacementRequest request);

        Task<UserFileReplacementResult>
            UpdatePortfolioFileViaProcAsync(
                UserFileReplacementRequest request);

        Task ResetPasswordViaProcAsync(
            Guid userId,
            string passwordHash);
    }
}
