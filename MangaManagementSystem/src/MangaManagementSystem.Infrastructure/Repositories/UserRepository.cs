using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository, IAdminUserQueryRepository, IUserProfileFileRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        private IQueryable<User> UsersWithRole()
        {
            return _context.Users
                .AsNoTracking()
                .Include(user => user.Role);
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

        public Task<User?> GetByIdWithRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return UsersWithRole()
                .SingleOrDefaultAsync(
                    user =>
                        user.UserId == userId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<User>>
            GetAllWithRoleAsync(
                CancellationToken cancellationToken = default)
        {
            return await UsersWithRole()
                .OrderByDescending(
                    user =>
                        user.CreatedAtUtc)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<PagedUserResult>
            SearchAdminUsersAsync(
                UserSearchCriteria criteria,
                CancellationToken cancellationToken = default)
        {
            var query =
                UsersWithRole();

            if (!string.IsNullOrWhiteSpace(
                    criteria.Search))
            {
                var search =
                    criteria.Search.Trim();

                query =
                    query.Where(
                        user =>
                            user.Username.Contains(search)
                            || user.Email.Contains(search)
                            || user.DisplayName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(
                    criteria.StatusCode))
            {
                var statusCode =
                    criteria.StatusCode.Trim();

                query =
                    query.Where(
                        user =>
                            user.StatusCode == statusCode);
            }

            if (!string.IsNullOrWhiteSpace(
                    criteria.RoleName))
            {
                var roleName =
                    criteria.RoleName.Trim();

                query =
                    query.Where(
                        user =>
                            user.Role != null
                            && user.Role.RoleName == roleName);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var items =
                await query
                    .OrderByDescending(
                        user =>
                            user.CreatedAtUtc)
                    .ThenBy(
                        user =>
                            user.Username)
                    .Skip(
                        (criteria.PageNumber - 1)
                        * criteria.PageSize)
                    .Take(
                        criteria.PageSize)
                    .ToListAsync(
                        cancellationToken);

            return new PagedUserResult(
                items,
                totalCount);
        }

        public async Task<IReadOnlyDictionary<string, int>>
            GetStatusCountsAsync(
                CancellationToken cancellationToken = default)
        {
            var rows =
                await _context.Users
                    .AsNoTracking()
                    .GroupBy(
                        user =>
                            user.StatusCode)
                    .Select(
                        group =>
                            new
                            {
                                StatusCode =
                                    group.Key,
                                Count =
                                    group.Count()
                            })
                    .ToListAsync(
                        cancellationToken);

            return rows.ToDictionary(
                row =>
                    row.StatusCode,
                row =>
                    row.Count,
                StringComparer.OrdinalIgnoreCase);
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

        public async Task<User?> GetByPortfolioFileIdAsync(
            Guid portfolioFileId)
        {
            if (portfolioFileId == Guid.Empty)
            {
                return null;
            }

            return await UsersWithRole()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user =>
                        user.PortfolioFileId ==
                        portfolioFileId);
        }

        public async Task ChangeUserStatusAsync(
            Guid adminUserId,
            Guid targetUserId,
            string newStatusCode,
            string? reason = null)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();

            var currentTransaction =
                _context.Database.CurrentTransaction;

            if (currentTransaction is not null)
            {
                cmd.Transaction =
                    currentTransaction.GetDbTransaction();
            }

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

        public async Task<Guid> CreateUserAsync(
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

        public async Task UpdateDisplayNameAsync(
            Guid userId,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "User id is required.",
                    nameof(userId));
            }

            string normalizedDisplayName =
                displayName?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    normalizedDisplayName))
            {
                throw new ArgumentException(
                    "Display name is required.",
                    nameof(displayName));
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        item => item.UserId == userId,
                        cancellationToken);

            if (user is null)
            {
                throw new System.Collections.Generic.KeyNotFoundException(
                    "The requested user could not be found.");
            }

            user.DisplayName =
                normalizedDisplayName;

            await _context.SaveChangesAsync(
                cancellationToken);
        }
        public Task<UserFileReplacementResult>
            ReplaceAvatarFileAsync(
                UserFileReplacementRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ReplaceUserProfileFileAsync(
                request,
                filePurposeCode: "USER_AVATAR",
                replaceAvatar: true,
                softDeleteReason:
                    "Replaced by a new user avatar.");
        }

        public Task<UserFileReplacementResult>
            ReplacePortfolioFileAsync(
                UserFileReplacementRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ReplaceUserProfileFileAsync(
                request,
                filePurposeCode:
                    "REGISTRATION_PORTFOLIO",
                replaceAvatar: false,
                softDeleteReason:
                    "Replaced by a new user portfolio.");
        }

        private async Task<UserFileReplacementResult>
            ReplaceUserProfileFileAsync(
                UserFileReplacementRequest request,
                string filePurposeCode,
                bool replaceAvatar,
                string softDeleteReason)
        {
            if (request.UserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "User id is required.",
                    nameof(request));
            }

            if (string.IsNullOrWhiteSpace(
                    request.OriginalFileName)
                || string.IsNullOrWhiteSpace(
                    request.CloudinaryPublicId)
                || string.IsNullOrWhiteSpace(
                    request.CloudinarySecureUrl)
                || string.IsNullOrWhiteSpace(
                    request.ContentType)
                || string.IsNullOrWhiteSpace(
                    request.Sha256Hash)
                || request.FileSizeBytes <= 0)
            {
                throw new ArgumentException(
                    "Uploaded file metadata is invalid.",
                    nameof(request));
            }

            IDbContextTransaction? transaction = null;

            if (_context.Database.CurrentTransaction is null)
            {
                transaction =
                    await _context.Database
                        .BeginTransactionAsync(
                            IsolationLevel.Serializable);
            }

            try
            {
                var user =
                    await _context.Users
                        .Include(item => item.Role)
                        .SingleOrDefaultAsync(
                            item =>
                                item.UserId
                                == request.UserId);

                if (user is null)
                {
                    throw new KeyNotFoundException(
                        "User was not found.");
                }

                if (user.Role is null)
                {
                    throw new InvalidOperationException(
                        "Actor user role could not be resolved.");
                }

                Guid? oldFileId =
                    replaceAvatar
                        ? user.AvatarFileId
                        : user.PortfolioFileId;

                FileResource? oldFile = null;

                if (oldFileId.HasValue)
                {
                    oldFile =
                        await _context.FileResources
                            .SingleOrDefaultAsync(
                                file =>
                                    file.FileResourceId
                                    == oldFileId.Value);

                    if (oldFile is null)
                    {
                        throw new InvalidOperationException(
                            replaceAvatar
                                ? "The previous avatar FileResource was not found."
                                : "The previous portfolio FileResource was not found.");
                    }

                    if (!string.Equals(
                            oldFile.FilePurposeCode,
                            filePurposeCode,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            replaceAvatar
                                ? "The previous file is not a USER_AVATAR resource."
                                : "The previous file is not a REGISTRATION_PORTFOLIO resource.");
                    }
                }

                var occurredAtUtc =
                    DateTime.UtcNow;

                var newFile =
                    new FileResource
                    {
                        FileResourceId =
                            Guid.NewGuid(),

                        FilePurposeCode =
                            filePurposeCode,

                        OriginalFileName =
                            request.OriginalFileName,

                        CloudinaryPublicId =
                            request.CloudinaryPublicId,

                        CloudinarySecureUrl =
                            request.CloudinarySecureUrl,

                        ContentType =
                            request.ContentType,

                        FileSizeBytes =
                            request.FileSizeBytes,

                        Sha256Hash =
                            request.Sha256Hash,

                        UploadedByUserId =
                            request.UserId,

                        UploadedAtUtc =
                            occurredAtUtc
                    };

                _context.FileResources.Add(
                    newFile);

                if (replaceAvatar)
                {
                    user.AvatarFileId =
                        newFile.FileResourceId;
                }
                else
                {
                    user.PortfolioFileId =
                        newFile.FileResourceId;
                }

                if (oldFile is not null
                    && !oldFile.DeletedAtUtc.HasValue)
                {
                    oldFile.DeletedAtUtc =
                        occurredAtUtc;

                    oldFile.DeletedByUserId =
                        request.UserId;

                    var fileDeleteDetailJson =
                        JsonSerializer.Serialize(
                            new
                            {
                                file_resource_id =
                                    oldFile.FileResourceId,

                                file_purpose_code =
                                    oldFile.FilePurposeCode,

                                original_file_name =
                                    oldFile.OriginalFileName,

                                cloudinary_public_id =
                                    oldFile.CloudinaryPublicId,

                                content_type =
                                    oldFile.ContentType,

                                delete_reason =
                                    softDeleteReason
                            });

                    _context.AuditEvents.Add(
                        new AuditEvent
                        {
                            OccurredAtUtc =
                                occurredAtUtc,

                            ActorUserId =
                                request.UserId,

                            ActorRoleName =
                                user.Role.RoleName,

                            ActionCode =
                                "FILE_RESOURCE_SOFT_DELETED",

                            EntityType =
                                "FileResource",

                            EntityId =
                                oldFile.FileResourceId
                                    .ToString(),

                            DetailJson =
                                fileDeleteDetailJson
                        });
                }

                var userActionCode =
                    replaceAvatar
                        ? "USER_AVATAR_UPDATED"
                        : oldFileId.HasValue
                            ? "USER_PORTFOLIO_UPDATED"
                            : "REGISTRATION_PORTFOLIO_ATTACHED";

                var userDetailJson =
                    replaceAvatar
                        ? JsonSerializer.Serialize(
                            new
                            {
                                user_id =
                                    request.UserId,

                                old_avatar_file_id =
                                    oldFileId,

                                new_avatar_file_id =
                                    newFile.FileResourceId,

                                old_cloudinary_public_id =
                                    oldFile?.CloudinaryPublicId,

                                new_cloudinary_public_id =
                                    newFile.CloudinaryPublicId,

                                new_original_file_name =
                                    newFile.OriginalFileName,

                                new_content_type =
                                    newFile.ContentType,

                                new_file_size_bytes =
                                    newFile.FileSizeBytes
                            })
                        : JsonSerializer.Serialize(
                            new
                            {
                                user_id =
                                    request.UserId,

                                old_portfolio_file_id =
                                    oldFileId,

                                new_portfolio_file_id =
                                    newFile.FileResourceId,

                                old_cloudinary_public_id =
                                    oldFile?.CloudinaryPublicId,

                                new_cloudinary_public_id =
                                    newFile.CloudinaryPublicId,

                                new_original_file_name =
                                    newFile.OriginalFileName,

                                new_content_type =
                                    newFile.ContentType,

                                new_file_size_bytes =
                                    newFile.FileSizeBytes
                            });

                _context.AuditEvents.Add(
                    new AuditEvent
                    {
                        OccurredAtUtc =
                            occurredAtUtc,

                        ActorUserId =
                            request.UserId,

                        ActorRoleName =
                            user.Role.RoleName,

                        ActionCode =
                            userActionCode,

                        EntityType =
                            "Users",

                        EntityId =
                            request.UserId.ToString(),

                        DetailJson =
                            userDetailJson
                    });

                await _context.SaveChangesAsync();

                if (transaction is not null)
                {
                    await transaction.CommitAsync();
                }

                return new UserFileReplacementResult(
                    newFile.FileResourceId,
                    oldFileId,
                    oldFile?.CloudinaryPublicId,
                    oldFile?.ContentType);
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        public async Task ResetPasswordAsync(
            Guid userId,
            string passwordHash)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "User id is required.",
                    nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(passwordHash)
                || passwordHash.Trim().Length < 20)
            {
                throw new ArgumentException(
                    "Password hash is invalid.",
                    nameof(passwordHash));
            }

            IDbContextTransaction? transaction = null;

            if (_context.Database.CurrentTransaction is null)
            {
                transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable);
            }

            try
            {
                var user =
                    await _context.Users
                        .SingleOrDefaultAsync(
                            item =>
                                item.UserId == userId);

                if (user is null)
                {
                    throw new KeyNotFoundException(
                        "Target user does not exist.");
                }

                var occurredAtUtc =
                    DateTime.UtcNow;

                user.PasswordHash =
                    passwordHash;

                var detailJson =
                    JsonSerializer.Serialize(
                        new
                        {
                            user_id =
                                user.UserId,

                            status_code =
                                user.StatusCode,

                            reset_mode =
                                "TOKEN_RESET",

                            reset_reason =
                                "Password reset verified by one-time token.",

                            result =
                                "Password hash updated."
                        });

                _context.AuditEvents.Add(
                    new AuditEvent
                    {
                        OccurredAtUtc =
                            occurredAtUtc,

                        ActorUserId =
                            null,

                        ActorRoleName =
                            null,

                        ActionCode =
                            "PASSWORD_RESET_BY_TOKEN",

                        EntityType =
                            "Users",

                        EntityId =
                            userId.ToString(),

                        DetailJson =
                            detailJson
                    });

                await _context.SaveChangesAsync();

                if (transaction is not null)
                {
                    await transaction.CommitAsync();
                }
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
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
