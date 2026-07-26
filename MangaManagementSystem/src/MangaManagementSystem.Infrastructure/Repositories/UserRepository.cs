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
            var result =
                await CreateUserCoreAsync(
                    roleName,
                    username,
                    email,
                    passwordHash,
                    displayName,
                    avatarFileId,
                    portfolioFileId,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    createdByUserId);

            return result.newUserId;
        }

        public Task<(
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
            return CreateUserCoreAsync(
                roleName,
                username,
                email,
                passwordHash,
                displayName,
                avatarFileId,
                null,
                portfolioOriginalFileName,
                portfolioCloudinaryPublicId,
                portfolioCloudinarySecureUrl,
                portfolioContentType,
                portfolioFileSizeBytes,
                portfolioSha256Hash,
                createdByUserId);
        }

        private async Task<(
            Guid newUserId,
            Guid? portfolioFileResourceId)>
            CreateUserCoreAsync(
                string roleName,
                string username,
                string email,
                string passwordHash,
                string? displayName,
                Guid? avatarFileId,
                Guid? existingPortfolioFileId,
                string? portfolioOriginalFileName,
                string? portfolioCloudinaryPublicId,
                string? portfolioCloudinarySecureUrl,
                string? portfolioContentType,
                long? portfolioFileSizeBytes,
                string? portfolioSha256Hash,
                Guid? createdByUserId)
        {
            var normalizedRoleName =
                roleName?.Trim()
                ?? string.Empty;

            var normalizedUsername =
                username?.Trim()
                ?? string.Empty;

            var normalizedEmail =
                email?.Trim()
                    .ToLowerInvariant()
                ?? string.Empty;

            var normalizedPasswordHash =
                passwordHash?.Trim()
                ?? string.Empty;

            var normalizedDisplayName =
                string.IsNullOrWhiteSpace(displayName)
                    ? normalizedUsername
                    : displayName.Trim();

            if (string.IsNullOrWhiteSpace(
                    normalizedRoleName))
            {
                throw new ArgumentException(
                    "Role name is required.",
                    nameof(roleName));
            }

            if (string.IsNullOrWhiteSpace(
                    normalizedUsername))
            {
                throw new ArgumentException(
                    "Username is required.",
                    nameof(username));
            }

            if (string.IsNullOrWhiteSpace(
                    normalizedEmail))
            {
                throw new ArgumentException(
                    "Email is required.",
                    nameof(email));
            }

            if (normalizedPasswordHash.Length < 20)
            {
                throw new ArgumentException(
                    "Password hash is invalid.",
                    nameof(passwordHash));
            }

            var hasNewPortfolio =
                !string.IsNullOrWhiteSpace(
                    portfolioCloudinaryPublicId);

            if (hasNewPortfolio
                && (
                    string.IsNullOrWhiteSpace(
                        portfolioOriginalFileName)
                    || string.IsNullOrWhiteSpace(
                        portfolioCloudinarySecureUrl)
                    || string.IsNullOrWhiteSpace(
                        portfolioContentType)
                    || !portfolioFileSizeBytes.HasValue
                    || portfolioFileSizeBytes.Value <= 0
                    || string.IsNullOrWhiteSpace(
                        portfolioSha256Hash)))
            {
                throw new ArgumentException(
                    "Portfolio file metadata is incomplete.");
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
                var normalizedRoleKey =
                    normalizedRoleName.ToUpper();

                var role =
                    await _context.Roles
                        .SingleOrDefaultAsync(
                            item =>
                                item.RoleName.ToUpper()
                                == normalizedRoleKey);

                if (role is null)
                {
                    throw new InvalidOperationException(
                        "Invalid role name.");
                }

                var normalizedUsernameKey =
                    normalizedUsername.ToUpper();

                var duplicateExists =
                    await _context.Users
                        .AsNoTracking()
                        .AnyAsync(
                            item =>
                                item.Username.ToUpper()
                                    == normalizedUsernameKey
                                || item.Email.ToLower()
                                    == normalizedEmail);

                if (duplicateExists)
                {
                    throw new InvalidOperationException(
                        "Username or email already exists.");
                }

                Guid auditActorUserId;
                string auditActorRoleName;

                if (createdByUserId.HasValue)
                {
                    var actor =
                        await _context.Users
                            .AsNoTracking()
                            .Include(item => item.Role)
                            .SingleOrDefaultAsync(
                                item =>
                                    item.UserId
                                    == createdByUserId.Value);

                    if (actor?.Role is null)
                    {
                        throw new InvalidOperationException(
                            "Creator user role could not be resolved.");
                    }

                    auditActorUserId =
                        actor.UserId;

                    auditActorRoleName =
                        actor.Role.RoleName;
                }
                else
                {
                    auditActorUserId =
                        Guid.Empty;

                    auditActorRoleName =
                        role.RoleName;
                }

                var occurredAtUtc =
                    DateTime.UtcNow;

                var newUserId =
                    Guid.NewGuid();

                if (!createdByUserId.HasValue)
                {
                    auditActorUserId =
                        newUserId;
                }

                var user =
                    new User
                    {
                        UserId =
                            newUserId,

                        RoleId =
                            role.RoleId,

                        Username =
                            normalizedUsername,

                        Email =
                            normalizedEmail,

                        PasswordHash =
                            normalizedPasswordHash,

                        DisplayName =
                            normalizedDisplayName,

                        AvatarFileId =
                            avatarFileId,

                        PortfolioFileId =
                            existingPortfolioFileId,

                        StatusCode =
                            "PENDING_APPROVAL",

                        CreatedAtUtc =
                            occurredAtUtc
                    };

                _context.Users.Add(
                    user);

                var registrationDetailJson =
                    JsonSerializer.Serialize(
                        new
                        {
                            user_id =
                                newUserId,

                            role_id =
                                role.RoleId,

                            role_name =
                                role.RoleName,

                            username =
                                normalizedUsername,

                            email =
                                normalizedEmail,

                            display_name =
                                normalizedDisplayName,

                            status_code =
                                "PENDING_APPROVAL"
                        });

                _context.AuditEvents.Add(
                    new AuditEvent
                    {
                        OccurredAtUtc =
                            occurredAtUtc,

                        ActorUserId =
                            auditActorUserId,

                        ActorRoleName =
                            auditActorRoleName,

                        ActionCode =
                            "USER_REGISTERED",

                        EntityType =
                            "Users",

                        EntityId =
                            newUserId.ToString(),

                        DetailJson =
                            registrationDetailJson
                    });

                /*
                    Save the User first.

                    A newly created portfolio references the User through
                    UploadedByUserId, while the User references the new
                    portfolio through PortfolioFileId. Saving the User first
                    avoids a circular insert dependency.
                */
                await _context.SaveChangesAsync();

                Guid? portfolioFileResourceId =
                    null;

                if (hasNewPortfolio)
                {
                    portfolioFileResourceId =
                        Guid.NewGuid();

                    var portfolioFile =
                        new FileResource
                        {
                            FileResourceId =
                                portfolioFileResourceId.Value,

                            FilePurposeCode =
                                "REGISTRATION_PORTFOLIO",

                            OriginalFileName =
                                portfolioOriginalFileName!.Trim(),

                            CloudinaryPublicId =
                                portfolioCloudinaryPublicId!.Trim(),

                            CloudinarySecureUrl =
                                portfolioCloudinarySecureUrl!.Trim(),

                            ContentType =
                                portfolioContentType!.Trim(),

                            FileSizeBytes =
                                portfolioFileSizeBytes!.Value,

                            Sha256Hash =
                                portfolioSha256Hash!.Trim(),

                            UploadedByUserId =
                                newUserId,

                            UploadedAtUtc =
                                occurredAtUtc
                        };

                    _context.FileResources.Add(
                        portfolioFile);

                    user.PortfolioFileId =
                        portfolioFileResourceId;

                    var portfolioDetailJson =
                        JsonSerializer.Serialize(
                            new
                            {
                                user_id =
                                    newUserId,

                                old_portfolio_file_id =
                                    (Guid?)null,

                                new_portfolio_file_id =
                                    portfolioFileResourceId,

                                old_cloudinary_public_id =
                                    (string?)null,

                                new_cloudinary_public_id =
                                    portfolioFile
                                        .CloudinaryPublicId,

                                new_original_file_name =
                                    portfolioFile
                                        .OriginalFileName,

                                new_content_type =
                                    portfolioFile
                                        .ContentType,

                                new_file_size_bytes =
                                    portfolioFile
                                        .FileSizeBytes
                            });

                    _context.AuditEvents.Add(
                        new AuditEvent
                        {
                            OccurredAtUtc =
                                occurredAtUtc,

                            ActorUserId =
                                newUserId,

                            ActorRoleName =
                                role.RoleName,

                            ActionCode =
                                "REGISTRATION_PORTFOLIO_ATTACHED",

                            EntityType =
                                "Users",

                            EntityId =
                                newUserId.ToString(),

                            DetailJson =
                                portfolioDetailJson
                        });

                    await _context.SaveChangesAsync();
                }

                if (transaction is not null)
                {
                    await transaction.CommitAsync();
                }

                return (
                    newUserId,
                    portfolioFileResourceId);
            }
            catch (Exception exception)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                if (exception is DbUpdateException)
                {
                    throw new InvalidOperationException(
                        "User creation failed because username, email, role, or related file data is invalid.",
                        exception);
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
