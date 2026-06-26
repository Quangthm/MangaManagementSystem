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
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        private IQueryable<User> UsersWithRole()
        {
            return _context.Users.Include(user => user.Role);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await UsersWithRole()
                .FirstOrDefaultAsync(user => user.Email == email);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await UsersWithRole()
                .FirstOrDefaultAsync(user => user.Username == username);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(
            string usernameOrEmail)
        {
            return await UsersWithRole()
                .FirstOrDefaultAsync(user =>
                    user.Email == usernameOrEmail
                    || user.Username == usernameOrEmail);
        }

        public async Task<IReadOnlyList<User>> GetByStatusAsync(
            string status)
        {
            return await UsersWithRole()
                .AsNoTracking()
                .Where(user => user.StatusCode == status)
                .OrderBy(user => user.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<User>> GetByRoleNameAsync(
            string roleName)
        {
            return await UsersWithRole()
                .AsNoTracking()
                .Where(user =>
                    user.Role != null
                    && user.Role.RoleName == roleName)
                .OrderBy(user => user.DisplayName)
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

            cmd.Parameters.Add(
                new SqlParameter(
                    "@admin_user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = adminUserId
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@target_user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = targetUserId
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@new_status_code",
                    SqlDbType.NVarChar,
                    30)
                {
                    Value = newStatusCode
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@reason",
                    SqlDbType.NVarChar,
                    500)
                {
                    Value = string.IsNullOrWhiteSpace(reason)
                        ? DBNull.Value
                        : reason.Trim()
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

            cmd.Parameters.Add(
                new SqlParameter(
                    "@role_name",
                    SqlDbType.NVarChar,
                    30)
                {
                    Value = roleName
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@username",
                    SqlDbType.NVarChar,
                    50)
                {
                    Value = username
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@email",
                    SqlDbType.NVarChar,
                    254)
                {
                    Value = email
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@password_hash",
                    SqlDbType.NVarChar,
                    255)
                {
                    Value = passwordHash
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@display_name",
                    SqlDbType.NVarChar,
                    100)
                {
                    Value = (object?)displayName ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@avatar_file_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = (object?)avatarFileId ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@portfolio_file_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = (object?)portfolioFileId ?? DBNull.Value
                });

            var outParam = new SqlParameter(
                "@new_user_id",
                SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(outParam);

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            return outParam.Value == DBNull.Value
                ? Guid.Empty
                : (Guid)outParam.Value;
        }

        public async Task<(
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
                Guid? createdByUserId = null)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                "auth.usp_User_CreateWithOptionalPortfolio";

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                new SqlParameter(
                    "@role_name",
                    SqlDbType.NVarChar,
                    30)
                {
                    Value = roleName
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@username",
                    SqlDbType.NVarChar,
                    50)
                {
                    Value = username
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@email",
                    SqlDbType.NVarChar,
                    254)
                {
                    Value = email
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@password_hash",
                    SqlDbType.NVarChar,
                    255)
                {
                    Value = passwordHash
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@display_name",
                    SqlDbType.NVarChar,
                    100)
                {
                    Value =
                        (object?)displayName
                        ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@avatar_file_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value =
                        (object?)avatarFileId
                        ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@portfolio_original_file_name",
                    SqlDbType.NVarChar,
                    260)
                {
                    Value =
                        (object?)portfolioOriginalFileName
                        ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@portfolio_cloudinary_public_id",
                    SqlDbType.NVarChar,
                    255)
                {
                    Value =
                        (object?)portfolioCloudinaryPublicId
                        ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@portfolio_cloudinary_secure_url",
                    SqlDbType.NVarChar,
                    1000)
                {
                    Value =
                        (object?)portfolioCloudinarySecureUrl
                        ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@portfolio_content_type",
                    SqlDbType.NVarChar,
                    100)
                {
                    Value =
                        (object?)portfolioContentType
                        ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@portfolio_file_size_bytes",
                    SqlDbType.BigInt)
                {
                    Value =
                        (object?)portfolioFileSizeBytes
                        ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@portfolio_sha256_hash",
                    SqlDbType.Char,
                    64)
                {
                    Value =
                        (object?)portfolioSha256Hash
                        ?? DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@created_by_user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value =
                        (object?)createdByUserId
                        ?? DBNull.Value
                });

            var outUserId = new SqlParameter(
                "@new_user_id",
                SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };

            var outFileResourceId = new SqlParameter(
                "@portfolio_file_resource_id",
                SqlDbType.UniqueIdentifier)
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

            var newUserId =
                outUserId.Value == DBNull.Value
                    ? Guid.Empty
                    : (Guid)outUserId.Value;

            var portfolioId =
                outFileResourceId.Value == DBNull.Value
                    ? (Guid?)null
                    : (Guid)outFileResourceId.Value;

            return (newUserId, portfolioId);
        }

        public async Task UpdateDisplayNameViaProcAsync(
            Guid userId,
            string displayName)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                "auth.usp_User_UpdateDisplayName";

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                new SqlParameter(
                    "@user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = userId
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@display_name",
                    SqlDbType.NVarChar,
                    100)
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

        public async Task<UserFileReplacementResult>
            UpdateAvatarFileViaProcAsync(
                UserFileReplacementRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                "auth.usp_User_UpdateAvatarFile";

            cmd.CommandType =
                CommandType.StoredProcedure;

            AddFileReplacementInputParameters(
                cmd,
                request);

            var newAvatarFileIdParameter =
                new SqlParameter(
                    "@new_avatar_file_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };

            var oldAvatarFileIdParameter =
                new SqlParameter(
                    "@old_avatar_file_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };

            var oldCloudinaryPublicIdParameter =
                new SqlParameter(
                    "@old_cloudinary_public_id",
                    SqlDbType.NVarChar,
                    255)
                {
                    Direction = ParameterDirection.Output
                };

            var oldContentTypeParameter =
                new SqlParameter(
                    "@old_content_type",
                    SqlDbType.NVarChar,
                    100)
                {
                    Direction = ParameterDirection.Output
                };

            cmd.Parameters.Add(newAvatarFileIdParameter);
            cmd.Parameters.Add(oldAvatarFileIdParameter);
            cmd.Parameters.Add(oldCloudinaryPublicIdParameter);
            cmd.Parameters.Add(oldContentTypeParameter);

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            if (newAvatarFileIdParameter.Value == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "The avatar update procedure did not return a new FileResource id.");
            }

            var result = new UserFileReplacementResult(
                (Guid)newAvatarFileIdParameter.Value,
                ReadNullableGuid(oldAvatarFileIdParameter),
                ReadNullableString(
                    oldCloudinaryPublicIdParameter),
                ReadNullableString(oldContentTypeParameter));

            await ReloadTrackedUserAsync(
                request.UserId);

            return result;
        }

        public async Task<UserFileReplacementResult>
            UpdatePortfolioFileViaProcAsync(
                UserFileReplacementRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                "auth.usp_User_UpdatePortfolioFile";

            cmd.CommandType =
                CommandType.StoredProcedure;

            AddFileReplacementInputParameters(
                cmd,
                request);

            var newPortfolioFileIdParameter =
                new SqlParameter(
                    "@new_portfolio_file_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };

            var oldPortfolioFileIdParameter =
                new SqlParameter(
                    "@old_portfolio_file_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };

            var oldCloudinaryPublicIdParameter =
                new SqlParameter(
                    "@old_cloudinary_public_id",
                    SqlDbType.NVarChar,
                    255)
                {
                    Direction = ParameterDirection.Output
                };

            var oldContentTypeParameter =
                new SqlParameter(
                    "@old_content_type",
                    SqlDbType.NVarChar,
                    100)
                {
                    Direction = ParameterDirection.Output
                };

            cmd.Parameters.Add(
                newPortfolioFileIdParameter);

            cmd.Parameters.Add(
                oldPortfolioFileIdParameter);

            cmd.Parameters.Add(
                oldCloudinaryPublicIdParameter);

            cmd.Parameters.Add(
                oldContentTypeParameter);

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            if (newPortfolioFileIdParameter.Value
                == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "The portfolio update procedure did not return a new FileResource id.");
            }

            var result =
                new UserFileReplacementResult(
                    (Guid)newPortfolioFileIdParameter.Value,
                    ReadNullableGuid(
                        oldPortfolioFileIdParameter),
                    ReadNullableString(
                        oldCloudinaryPublicIdParameter),
                    ReadNullableString(
                        oldContentTypeParameter));

            await ReloadTrackedUserAsync(
                request.UserId);

            return result;
        }

        public async Task ResetPasswordViaProcAsync(
            Guid userId,
            string passwordHash)
        {
            var conn = _context.Database.GetDbConnection();

            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                "auth.usp_User_ResetPassword";

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                new SqlParameter(
                    "@target_user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = userId
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@new_password_hash",
                    SqlDbType.NVarChar,
                    255)
                {
                    Value = passwordHash
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@actor_user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = DBNull.Value
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@reset_mode",
                    SqlDbType.NVarChar,
                    30)
                {
                    Value = "TOKEN_RESET"
                });

            cmd.Parameters.Add(
                new SqlParameter(
                    "@reset_reason",
                    SqlDbType.NVarChar,
                    500)
                {
                    Value = "Password reset verified by OTP."
                });

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await cmd.ExecuteNonQueryAsync();

            await ReloadTrackedUserAsync(userId);
        }

        private static void AddFileReplacementInputParameters(
            System.Data.Common.DbCommand command,
            UserFileReplacementRequest request)
        {
            command.Parameters.Add(
                new SqlParameter(
                    "@user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value = request.UserId
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@original_file_name",
                    SqlDbType.NVarChar,
                    260)
                {
                    Value = request.OriginalFileName
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@cloudinary_public_id",
                    SqlDbType.NVarChar,
                    255)
                {
                    Value = request.CloudinaryPublicId
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@cloudinary_secure_url",
                    SqlDbType.NVarChar,
                    1000)
                {
                    Value = request.CloudinarySecureUrl
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@content_type",
                    SqlDbType.NVarChar,
                    100)
                {
                    Value = request.ContentType
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@file_size_bytes",
                    SqlDbType.BigInt)
                {
                    Value = request.FileSizeBytes
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@sha256_hash",
                    SqlDbType.Char,
                    64)
                {
                    Value = request.Sha256Hash
                });
        }

        private static Guid? ReadNullableGuid(
            SqlParameter parameter)
        {
            return parameter.Value == DBNull.Value
                ? null
                : (Guid)parameter.Value;
        }

        private static string? ReadNullableString(
            SqlParameter parameter)
        {
            return parameter.Value == DBNull.Value
                ? null
                : Convert.ToString(parameter.Value);
        }

        private async Task ReloadTrackedUserAsync(
            Guid userId)
        {
            var trackedUser =
                _context.Users.Local
                    .FirstOrDefault(
                        user => user.UserId == userId);

            if (trackedUser != null)
            {
                await _context.Entry(trackedUser)
                    .ReloadAsync();
            }
        }
    }
}
