using MangaManagementSystem.Application.Interfaces;
using System.Data;
using System.Text.Json;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public sealed class FileResourceRepository
        : GenericRepository<FileResource>,
          IFileResourceRepository,
          IAdminFileResourceRepository
    {
        public FileResourceRepository(
            ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<(
            IReadOnlyList<AdminFileResourceListItem> Items,
            int TotalCount)> SearchAdminAsync(
                AdminFileResourceSearchCriteria criteria,
                CancellationToken cancellationToken = default)
        {
            var search =
                string.IsNullOrWhiteSpace(criteria.Search)
                    ? null
                    : criteria.Search.Trim();

            var filePurposeCode =
                string.IsNullOrWhiteSpace(criteria.FilePurposeCode)
                    ? null
                    : criteria.FilePurposeCode.Trim();

            var deletedState =
                string.IsNullOrWhiteSpace(criteria.DeletedState)
                    ? AdminFileDeletionStates.ActiveNormalized
                    : criteria.DeletedState
                        .Trim()
                        .ToLowerInvariant();

            var pageNumber =
                criteria.PageNumber <= 0
                    ? 1
                    : criteria.PageNumber;

            var pageSize =
                criteria.PageSize <= 0
                    ? 20
                    : criteria.PageSize;

            var query =
                _context
                    .Set<FileResource>()
                    .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query =
                    query.Where(file =>
                        file.OriginalFileName.Contains(search)
                        || file.FilePurposeCode.Contains(search)
                        || file.ContentType.Contains(search)
                        || (
                            file.Sha256Hash != null
                            && file.Sha256Hash.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filePurposeCode))
            {
                query =
                    query.Where(file =>
                        file.FilePurposeCode == filePurposeCode);
            }

            query = deletedState switch
            {
                AdminFileDeletionStates.ActiveNormalized =>
                    query.Where(file =>
                        file.DeletedAtUtc == null),

                AdminFileDeletionStates.DeletedNormalized =>
                    query.Where(file =>
                        file.DeletedAtUtc != null),

                AdminFileDeletionStates.AllNormalized =>
                    query,

                _ =>
                    query.Where(file =>
                        file.DeletedAtUtc == null)
            };

            if (criteria.FromUtc.HasValue)
            {
                query =
                    query.Where(file =>
                        file.UploadedAtUtc >= criteria.FromUtc.Value);
            }

            if (criteria.ToUtc.HasValue)
            {
                query =
                    query.Where(file =>
                        file.UploadedAtUtc <= criteria.ToUtc.Value);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var skip =
                (pageNumber - 1) * pageSize;

            var pagedFiles =
                query
                    .OrderByDescending(file =>
                        file.UploadedAtUtc)
                    .ThenBy(file =>
                        file.OriginalFileName)
                    .Skip(skip)
                    .Take(pageSize);

            var items =
                await (
                    from file in pagedFiles
                    join uploadedBy in _context.Users.AsNoTracking()
                        on file.UploadedByUserId equals uploadedBy.UserId
                        into uploadedUsers
                    from uploadedBy in uploadedUsers.DefaultIfEmpty()
                    join deletedBy in _context.Users.AsNoTracking()
                        on file.DeletedByUserId equals deletedBy.UserId
                        into deletedUsers
                    from deletedBy in deletedUsers.DefaultIfEmpty()
                    select new AdminFileResourceListItem(
                        file.FileResourceId,
                        file.FilePurposeCode,
                        file.OriginalFileName,
                        file.ContentType,
                        file.FileSizeBytes,
                        file.Sha256Hash ?? string.Empty,
                        file.UploadedByUserId,
                        uploadedBy == null
                            ? null
                            : uploadedBy.Username,
                        uploadedBy == null
                            ? null
                            : uploadedBy.DisplayName,
                        file.UploadedAtUtc,
                        file.DeletedAtUtc,
                        file.DeletedByUserId,
                        deletedBy == null
                            ? null
                            : deletedBy.Username,
                        deletedBy == null
                            ? null
                            : deletedBy.DisplayName))
                .ToListAsync(
                    cancellationToken);

            return (
                items,
                totalCount);
        }

        public async Task<AdminFileResourceDetail?>
            GetAdminByIdAsync(
                Guid actorUserId,
                Guid fileResourceId,
                CancellationToken cancellationToken = default)
        {
            var detail =
                await (
                    from file in _context
                        .Set<FileResource>()
                        .AsNoTracking()
                    where file.FileResourceId == fileResourceId
                    join uploadedBy in _context.Users.AsNoTracking()
                        on file.UploadedByUserId equals uploadedBy.UserId
                        into uploadedUsers
                    from uploadedBy in uploadedUsers.DefaultIfEmpty()
                    join deletedBy in _context.Users.AsNoTracking()
                        on file.DeletedByUserId equals deletedBy.UserId
                        into deletedUsers
                    from deletedBy in deletedUsers.DefaultIfEmpty()
                    select new
                    {
                        file.FileResourceId,
                        file.FilePurposeCode,
                        file.OriginalFileName,
                        file.CloudinaryPublicId,
                        file.CloudinarySecureUrl,
                        file.ContentType,
                        file.FileSizeBytes,
                        file.Sha256Hash,
                        file.UploadedByUserId,
                        UploadedByUsername =
                            uploadedBy == null
                                ? null
                                : uploadedBy.Username,
                        UploadedByDisplayName =
                            uploadedBy == null
                                ? null
                                : uploadedBy.DisplayName,
                        file.UploadedAtUtc,
                        file.DeletedAtUtc,
                        file.DeletedByUserId,
                        DeletedByUsername =
                            deletedBy == null
                                ? null
                                : deletedBy.Username,
                        DeletedByDisplayName =
                            deletedBy == null
                                ? null
                                : deletedBy.DisplayName
                    })
                .FirstOrDefaultAsync(
                    cancellationToken);

            if (detail == null)
            {
                return null;
            }

            var referenceCount =
                await _context.Users
                    .AsNoTracking()
                    .LongCountAsync(
                        user =>
                            user.AvatarFileId == fileResourceId
                            || user.PortfolioFileId == fileResourceId,
                        cancellationToken);

            return new AdminFileResourceDetail(
                detail.FileResourceId,
                detail.FilePurposeCode,
                detail.OriginalFileName,
                detail.CloudinaryPublicId,
                detail.CloudinarySecureUrl,
                detail.ContentType,
                detail.FileSizeBytes,
                detail.Sha256Hash ?? string.Empty,
                detail.UploadedByUserId,
                detail.UploadedByUsername,
                detail.UploadedByDisplayName,
                DateTime.SpecifyKind(
                    detail.UploadedAtUtc,
                    DateTimeKind.Utc),
                detail.DeletedAtUtc.HasValue
                    ? DateTime.SpecifyKind(
                        detail.DeletedAtUtc.Value,
                        DateTimeKind.Utc)
                    : null,
                detail.DeletedByUserId,
                detail.DeletedByUsername,
                detail.DeletedByDisplayName,
                referenceCount);
        }

        public async Task SoftDeleteAdminAsync(
            Guid actorUserId,
            Guid fileResourceId,
            string deleteReason,
            CancellationToken cancellationToken = default)
        {
            var normalizedDeleteReason =
                string.IsNullOrWhiteSpace(deleteReason)
                    ? null
                    : deleteReason.Trim();

            IDbContextTransaction? transaction = null;

            if (_context.Database.CurrentTransaction is null)
            {
                transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);
            }

            try
            {
                var actor =
                    await _context.Users
                        .AsNoTracking()
                        .Include(user => user.Role)
                        .SingleOrDefaultAsync(
                            user =>
                                user.UserId == actorUserId,
                            cancellationToken);

                if (actor?.Role is null)
                {
                    throw new InvalidOperationException(
                        "Actor user role could not be resolved.");
                }

                var fileResource =
                    await _context.FileResources
                        .SingleOrDefaultAsync(
                            file =>
                                file.FileResourceId
                                == fileResourceId,
                            cancellationToken);

                if (fileResource is null)
                {
                    throw new KeyNotFoundException(
                        "File resource does not exist.");
                }

                if (fileResource.DeletedAtUtc.HasValue)
                {
                    throw new InvalidOperationException(
                        "File resource is already deleted.");
                }

                var deletedAtUtc =
                    DateTime.UtcNow;

                fileResource.DeletedAtUtc =
                    deletedAtUtc;

                fileResource.DeletedByUserId =
                    actorUserId;

                var detailJson =
                    JsonSerializer.Serialize(
                        new
                        {
                            file_resource_id =
                                fileResource.FileResourceId,

                            file_purpose_code =
                                fileResource.FilePurposeCode,

                            original_file_name =
                                fileResource.OriginalFileName,

                            cloudinary_public_id =
                                fileResource.CloudinaryPublicId,

                            content_type =
                                fileResource.ContentType,

                            delete_reason =
                                normalizedDeleteReason
                        });

                _context.AuditEvents.Add(
                    new AuditEvent
                    {
                        OccurredAtUtc =
                            deletedAtUtc,

                        ActorUserId =
                            actorUserId,

                        ActorRoleName =
                            actor.Role.RoleName,

                        ActionCode =
                            "FILE_RESOURCE_SOFT_DELETED",

                        EntityType =
                            "FileResource",

                        EntityId =
                            fileResourceId.ToString(),

                        DetailJson =
                            detailJson
                    });

                await _context.SaveChangesAsync(
                    cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(
                        cancellationToken);
                }
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(
                        cancellationToken);
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
    }
}
