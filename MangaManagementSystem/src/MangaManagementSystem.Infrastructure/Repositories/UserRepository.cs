using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
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
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@admin_user_id", SqlDbType.UniqueIdentifier)
            {
                Value = adminUserId
            });

            cmd.Parameters.Add(new SqlParameter("@target_user_id", SqlDbType.UniqueIdentifier)
            {
                Value = targetUserId
            });

            cmd.Parameters.Add(new SqlParameter("@new_status_code", SqlDbType.NVarChar, 30)
            {
                Value = newStatusCode
            });

            cmd.Parameters.Add(new SqlParameter("@reason", SqlDbType.NVarChar, 500)
            {
                Value = string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason.Trim()
            });

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            await ReloadTrackedUserAsync(targetUserId);
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
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = "auth.usp_User_Create";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@role_name", SqlDbType.NVarChar, 30)
            {
                Value = roleName
            });

            cmd.Parameters.Add(new SqlParameter("@username", SqlDbType.NVarChar, 50)
            {
                Value = username
            });

            cmd.Parameters.Add(new SqlParameter("@email", SqlDbType.NVarChar, 254)
            {
                Value = email
            });

            cmd.Parameters.Add(new SqlParameter("@password_hash", SqlDbType.NVarChar, 255)
            {
                Value = passwordHash
            });

            cmd.Parameters.Add(new SqlParameter("@display_name", SqlDbType.NVarChar, 100)
            {
                Value = (object?)displayName ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@avatar_file_id", SqlDbType.UniqueIdentifier)
            {
                Value = (object?)avatarFileId ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@portfolio_file_id", SqlDbType.UniqueIdentifier)
            {
                Value = (object?)portfolioFileId ?? DBNull.Value
            });

            var outParam = new SqlParameter("@new_user_id", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(outParam);

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            return outParam.Value == DBNull.Value ? Guid.Empty : (Guid)outParam.Value;
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
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@role_name", SqlDbType.NVarChar, 30)
            {
                Value = roleName
            });

            cmd.Parameters.Add(new SqlParameter("@username", SqlDbType.NVarChar, 50)
            {
                Value = username
            });

            cmd.Parameters.Add(new SqlParameter("@email", SqlDbType.NVarChar, 254)
            {
                Value = email
            });

            cmd.Parameters.Add(new SqlParameter("@password_hash", SqlDbType.NVarChar, 255)
            {
                Value = passwordHash
            });

            cmd.Parameters.Add(new SqlParameter("@display_name", SqlDbType.NVarChar, 100)
            {
                Value = (object?)displayName ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@avatar_file_id", SqlDbType.UniqueIdentifier)
            {
                Value = (object?)avatarFileId ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@portfolio_original_file_name", SqlDbType.NVarChar, 260)
            {
                Value = (object?)portfolioOriginalFileName ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@portfolio_cloudinary_public_id", SqlDbType.NVarChar, 255)
            {
                Value = (object?)portfolioCloudinaryPublicId ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@portfolio_cloudinary_secure_url", SqlDbType.NVarChar, 1000)
            {
                Value = (object?)portfolioCloudinarySecureUrl ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@portfolio_content_type", SqlDbType.NVarChar, 100)
            {
                Value = (object?)portfolioContentType ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@portfolio_file_size_bytes", SqlDbType.BigInt)
            {
                Value = (object?)portfolioFileSizeBytes ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@portfolio_sha256_hash", SqlDbType.Char, 64)
            {
                Value = (object?)portfolioSha256Hash ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@created_by_user_id", SqlDbType.UniqueIdentifier)
            {
                Value = (object?)createdByUserId ?? DBNull.Value
            });

            var outUserId = new SqlParameter("@new_user_id", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };

            var outFileResourceId = new SqlParameter("@portfolio_file_resource_id", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(outUserId);
            cmd.Parameters.Add(outFileResourceId);

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            var newUserId = outUserId.Value == DBNull.Value ? Guid.Empty : (Guid)outUserId.Value;
            var portfolioId = outFileResourceId.Value == DBNull.Value ? (Guid?)null : (Guid)outFileResourceId.Value;

            return (newUserId, portfolioId);
        }

        public async Task UpdateDisplayNameViaProcAsync(Guid userId, string displayName)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = "auth.usp_User_UpdateDisplayName";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@user_id", SqlDbType.UniqueIdentifier)
            {
                Value = userId
            });

            cmd.Parameters.Add(new SqlParameter("@display_name", SqlDbType.NVarChar, 100)
            {
                Value = displayName.Trim()
            });

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            await ReloadTrackedUserAsync(userId);
        }

        public async Task UpdateAvatarFileViaProcAsync(Guid userId, Guid avatarFileId)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = "auth.usp_User_UpdateAvatarFile";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@user_id", SqlDbType.UniqueIdentifier)
            {
                Value = userId
            });

            cmd.Parameters.Add(new SqlParameter("@avatar_file_id", SqlDbType.UniqueIdentifier)
            {
                Value = avatarFileId
            });

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            await ReloadTrackedUserAsync(userId);
        }

        public async Task UpdatePortfolioFileViaProcAsync(Guid userId, Guid portfolioFileId)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = "auth.usp_User_UpdatePortfolioFile";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@user_id", SqlDbType.UniqueIdentifier)
            {
                Value = userId
            });

            cmd.Parameters.Add(new SqlParameter("@portfolio_file_id", SqlDbType.UniqueIdentifier)
            {
                Value = portfolioFileId
            });

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            await ReloadTrackedUserAsync(userId);
        }

        public async Task ResetPasswordViaProcAsync(Guid userId, string passwordHash)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = "auth.usp_User_ResetPassword";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@user_id", SqlDbType.UniqueIdentifier)
            {
                Value = userId
            });

            cmd.Parameters.Add(new SqlParameter("@password_hash", SqlDbType.NVarChar, 255)
            {
                Value = passwordHash
            });

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            await ReloadTrackedUserAsync(userId);
        }

        private async Task ReloadTrackedUserAsync(Guid userId)
        {
            var trackedUser = _context.Users.Local.FirstOrDefault(u => u.UserId == userId);

            if (trackedUser != null)
            {
                await _context.Entry(trackedUser).ReloadAsync();
            }
        }
    }
}