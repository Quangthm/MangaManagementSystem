using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == usernameOrEmail || u.Username == usernameOrEmail);
        }

        public async Task<IReadOnlyList<User>> GetByStatusAsync(string status)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.StatusCode == status)
                .OrderBy(u => u.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task ChangeUserStatusViaProcAsync(
            Guid adminUserId,
            Guid targetUserId,
            string newStatusCode,
            string? reason = null)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "auth.usp_Admin_ChangeUserStatus";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@admin_user_id", System.Data.SqlDbType.UniqueIdentifier)
            {
                Value = adminUserId
            });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@target_user_id", System.Data.SqlDbType.UniqueIdentifier)
            {
                Value = targetUserId
            });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@new_status_code", System.Data.SqlDbType.NVarChar, 30)
            {
                Value = newStatusCode
            });
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@reason", System.Data.SqlDbType.NVarChar, 500)
            {
                Value = string.IsNullOrWhiteSpace(reason) ? System.DBNull.Value : reason.Trim()
            });

            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            var trackedUser = _context.Users.Local.FirstOrDefault(u => u.UserId == targetUserId);
            if (trackedUser != null)
            {
                await _context.Entry(trackedUser).ReloadAsync();
            }
        }

        public async Task<Guid> CreateUserViaProcAsync(
        string roleName,
        string username,
        string email,
        string passwordHash,
        string? displayName = null,
        Guid? avatarFileId = null,
        Guid? portfolioFileId = null,
        Guid? createdByUserId = null)
    {
        // Use parameterized raw SQL to call stored procedure and return new_user_id
        var conn = _context.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "auth.usp_User_Create";
        cmd.CommandType = System.Data.CommandType.StoredProcedure;

        var pRole = new Microsoft.Data.SqlClient.SqlParameter("@role_name", System.Data.SqlDbType.NVarChar, 30) { Value = (object)roleName };
        var pUsername = new Microsoft.Data.SqlClient.SqlParameter("@username", System.Data.SqlDbType.NVarChar, 50) { Value = (object)username };
        var pEmail = new Microsoft.Data.SqlClient.SqlParameter("@email", System.Data.SqlDbType.NVarChar, 254) { Value = (object)email };
        var pPasswordHash = new Microsoft.Data.SqlClient.SqlParameter("@password_hash", System.Data.SqlDbType.NVarChar, 255) { Value = (object)passwordHash };
        var pDisplayName = new Microsoft.Data.SqlClient.SqlParameter("@display_name", System.Data.SqlDbType.NVarChar, 100) { Value = (object?)displayName ?? System.DBNull.Value };
        var pAvatarFileId = new Microsoft.Data.SqlClient.SqlParameter("@avatar_file_id", System.Data.SqlDbType.UniqueIdentifier) { Value = (object?)avatarFileId ?? System.DBNull.Value };

        cmd.Parameters.Add(pRole);
        cmd.Parameters.Add(pUsername);
        cmd.Parameters.Add(pEmail);
        cmd.Parameters.Add(pPasswordHash);
        cmd.Parameters.Add(pDisplayName);
        cmd.Parameters.Add(pAvatarFileId);
        cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@portfolio_file_id", (object?)portfolioFileId ?? System.DBNull.Value));

        var outParam = new Microsoft.Data.SqlClient.SqlParameter("@new_user_id", System.Data.SqlDbType.UniqueIdentifier) { Direction = System.Data.ParameterDirection.Output };
        cmd.Parameters.Add(outParam);

        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await cmd.ExecuteNonQueryAsync();

        var newUserId = outParam.Value == System.DBNull.Value ? Guid.Empty : (Guid)outParam.Value;
        return newUserId;
    }

        public async Task<(Guid newUserId, Guid? portfolioFileResourceId)> CreateUserWithOptionalPortfolioAsync(
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
            Guid? createdByUserId = null)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "auth.usp_User_CreateWithOptionalPortfolio";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@role_name", roleName));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@username", username));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@email", email));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@password_hash", passwordHash));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@display_name", (object?)displayName ?? System.DBNull.Value));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@avatar_file_id", (object?)avatarFileId ?? System.DBNull.Value));

            // portfolio metadata
            var pPortfolioOriginalFileName = new Microsoft.Data.SqlClient.SqlParameter("@portfolio_original_file_name", System.Data.SqlDbType.NVarChar, 260) { Value = (object?)portfolioOriginalFileName ?? System.DBNull.Value };
            var pPortfolioPublicId = new Microsoft.Data.SqlClient.SqlParameter("@portfolio_cloudinary_public_id", System.Data.SqlDbType.NVarChar, 255) { Value = (object?)portfolioCloudinaryPublicId ?? System.DBNull.Value };
            var pPortfolioSecureUrl = new Microsoft.Data.SqlClient.SqlParameter("@portfolio_cloudinary_secure_url", System.Data.SqlDbType.NVarChar, 1000) { Value = (object?)portfolioCloudinarySecureUrl ?? System.DBNull.Value };
            var pPortfolioContentType = new Microsoft.Data.SqlClient.SqlParameter("@portfolio_content_type", System.Data.SqlDbType.NVarChar, 100) { Value = (object?)portfolioContentType ?? System.DBNull.Value };
            var pPortfolioFileSize = new Microsoft.Data.SqlClient.SqlParameter("@portfolio_file_size_bytes", System.Data.SqlDbType.BigInt) { Value = (object?)portfolioFileSizeBytes ?? System.DBNull.Value };
            var pPortfolioSha256 = new Microsoft.Data.SqlClient.SqlParameter("@portfolio_sha256_hash", System.Data.SqlDbType.Char, 64) { Value = (object?)portfolioSha256Hash ?? System.DBNull.Value };

            cmd.Parameters.Add(pPortfolioOriginalFileName);
            cmd.Parameters.Add(pPortfolioPublicId);
            cmd.Parameters.Add(pPortfolioSecureUrl);
            cmd.Parameters.Add(pPortfolioContentType);
            cmd.Parameters.Add(pPortfolioFileSize);
            cmd.Parameters.Add(pPortfolioSha256);

            var pCreatedBy = new Microsoft.Data.SqlClient.SqlParameter("@created_by_user_id", System.Data.SqlDbType.UniqueIdentifier) { Value = (object?)createdByUserId ?? System.DBNull.Value };
            cmd.Parameters.Add(pCreatedBy);

            var outUserId = new Microsoft.Data.SqlClient.SqlParameter("@new_user_id", System.Data.SqlDbType.UniqueIdentifier) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outUserId);

            var outFileResourceId = new Microsoft.Data.SqlClient.SqlParameter("@portfolio_file_resource_id", System.Data.SqlDbType.UniqueIdentifier) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outFileResourceId);

            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();

            var newUserId = outUserId.Value == System.DBNull.Value ? Guid.Empty : (Guid)outUserId.Value;
            var portfolioId = outFileResourceId.Value == System.DBNull.Value ? (Guid?)null : (Guid)outFileResourceId.Value;
            return (newUserId, portfolioId);
        }
    }
}
