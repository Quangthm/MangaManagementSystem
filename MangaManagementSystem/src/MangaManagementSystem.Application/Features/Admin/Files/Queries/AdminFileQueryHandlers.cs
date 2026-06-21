using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Files.Queries
{
    internal static class AdminFileValidation
    {
        private static readonly HashSet<string>
            AllowedDeletedStates =
                new(
                    StringComparer.OrdinalIgnoreCase)
                {
                    "ACTIVE",
                    "DELETED",
                    "ALL"
                };

        internal static Guid ValidateActorUserId(
            Guid actorUserId)
        {
            if (actorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Actor user id is required.");
            }

            return actorUserId;
        }

        internal static Guid ValidateFileResourceId(
            Guid fileResourceId)
        {
            if (fileResourceId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "File resource id is required.");
            }

            return fileResourceId;
        }

        internal static void ValidatePage(
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1)
            {
                throw new InvalidOperationException(
                    "Page number must be greater than zero.");
            }

            if (pageSize is < 1 or > 100)
            {
                throw new InvalidOperationException(
                    "Page size must be between 1 and 100.");
            }
        }

        internal static void ValidateDates(
            DateTime? fromUtc,
            DateTime? toUtc)
        {
            if (fromUtc.HasValue
                && toUtc.HasValue
                && fromUtc.Value > toUtc.Value)
            {
                throw new InvalidOperationException(
                    "The from date cannot be later than the to date.");
            }
        }

        internal static string? NormalizeOptionalText(
            string? value,
            string fieldName,
            int maximumLength,
            bool uppercase = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized =
                value.Trim();

            if (normalized.Length > maximumLength)
            {
                throw new InvalidOperationException(
                    $"{fieldName} cannot exceed {maximumLength} characters.");
            }

            return uppercase
                ? normalized.ToUpperInvariant()
                : normalized;
        }

        internal static string NormalizeDeletedState(
            string? value)
        {
            var normalized =
                string.IsNullOrWhiteSpace(value)
                    ? "ACTIVE"
                    : value.Trim().ToUpperInvariant();

            if (!AllowedDeletedStates.Contains(
                    normalized))
            {
                throw new InvalidOperationException(
                    "Deleted state must be ACTIVE, DELETED, or ALL.");
            }

            return normalized;
        }

        internal static string NormalizeDeleteReason(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Delete reason is required.");
            }

            var normalized =
                value.Trim();

            if (normalized.Length > 500)
            {
                throw new InvalidOperationException(
                    "Delete reason cannot exceed 500 characters.");
            }

            return normalized;
        }
    }

    internal static class AdminFileDtoMapper
    {
        private static readonly HashSet<string>
            PreviewableContentTypes =
                new(
                    StringComparer.OrdinalIgnoreCase)
                {
                    "image/png",
                    "image/jpeg",
                    "image/jpg",
                    "image/gif",
                    "image/webp",
                    "image/svg+xml",
                    "application/pdf"
                };

        internal static AdminFileListItemDto ToListItemDto(
            AdminFileResourceListItem item)
        {
            return new AdminFileListItemDto(
                item.FileResourceId,
                item.FilePurposeCode,
                item.OriginalFileName,
                item.ContentType,
                item.FileSizeBytes,
                item.Sha256Hash,
                item.UploadedByUserId,
                item.UploadedByUsername,
                item.UploadedByDisplayName,
                item.UploadedAtUtc,
                item.DeletedAtUtc,
                item.DeletedByUserId,
                item.DeletedByUsername,
                item.DeletedByDisplayName,
                item.StorageCleanupStatus,
                item.StorageCleanedAtUtc,
                item.StorageCleanedByUserId,
                item.StorageCleanedByUsername,
                item.StorageCleanedByDisplayName,
                item.StorageCleanupError,
                item.DeletedAtUtc.HasValue);
        }

        internal static AdminFileDetailDto ToDetailDto(
            AdminFileResourceDetail item)
        {
            var isDeleted =
                item.DeletedAtUtc.HasValue;

            var canPreview =
                isDeleted
                || PreviewableContentTypes.Contains(
                    item.ContentType);

            return new AdminFileDetailDto(
                item.FileResourceId,
                item.FilePurposeCode,
                item.OriginalFileName,
                item.ContentType,
                item.FileSizeBytes,
                item.Sha256Hash,
                item.UploadedByUserId,
                item.UploadedByUsername,
                item.UploadedByDisplayName,
                item.UploadedAtUtc,
                item.DeletedAtUtc,
                item.DeletedByUserId,
                item.DeletedByUsername,
                item.DeletedByDisplayName,
                item.StorageCleanupStatus,
                item.StorageCleanedAtUtc,
                item.StorageCleanedByUserId,
                item.StorageCleanedByUsername,
                item.StorageCleanedByDisplayName,
                item.StorageCleanupError,
                item.ReferenceCount,
                isDeleted,
                canPreview,
                !isDeleted);
        }

        internal static AdminFileContentSourceDto
            ToContentSourceDto(
                AdminFileResourceDetail item)
        {
            var isDeleted =
                item.DeletedAtUtc.HasValue;

            return new AdminFileContentSourceDto(
                item.FileResourceId,
                item.OriginalFileName,
                item.CloudinarySecureUrl,
                item.ContentType,
                item.FileSizeBytes,
                item.DeletedAtUtc,
                item.StorageCleanupStatus,
                item.StorageCleanedAtUtc,
                isDeleted,
                !isDeleted
                    && PreviewableContentTypes.Contains(
                        item.ContentType));
        }
    }

    public sealed class SearchAdminFilesQueryHandler
        : IRequestHandler<
            SearchAdminFilesQuery,
            AdminFilePageDto>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;

        public SearchAdminFilesQueryHandler(
            IFileResourceRepository fileResourceRepository)
        {
            _fileResourceRepository =
                fileResourceRepository;
        }

        public async Task<AdminFilePageDto> Handle(
            SearchAdminFilesQuery request,
            CancellationToken cancellationToken)
        {
            AdminFileValidation.ValidateActorUserId(
                request.ActorUserId);

            AdminFileValidation.ValidatePage(
                request.PageNumber,
                request.PageSize);

            AdminFileValidation.ValidateDates(
                request.FromUtc,
                request.ToUtc);

            var criteria =
                new AdminFileResourceSearchCriteria(
                    request.ActorUserId,
                    AdminFileValidation.NormalizeOptionalText(
                        request.Search,
                        "Search text",
                        260),
                    AdminFileValidation.NormalizeOptionalText(
                        request.FilePurposeCode,
                        "File purpose code",
                        50,
                        uppercase: true),
                    AdminFileValidation.NormalizeDeletedState(
                        request.DeletedState),
                    request.FromUtc,
                    request.ToUtc,
                    request.PageNumber,
                    request.PageSize);

            var result =
                await _fileResourceRepository
                    .SearchAdminAsync(
                        criteria,
                        cancellationToken);

            var totalPages =
                result.TotalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        result.TotalCount
                        / (double)request.PageSize);

            var items =
                result.Items
                    .Select(
                        AdminFileDtoMapper
                            .ToListItemDto)
                    .ToList();

            return new AdminFilePageDto(
                items,
                request.PageNumber,
                request.PageSize,
                result.TotalCount,
                totalPages);
        }
    }

    public sealed class GetAdminFileDetailQueryHandler
        : IRequestHandler<
            GetAdminFileDetailQuery,
            AdminFileDetailDto?>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;

        public GetAdminFileDetailQueryHandler(
            IFileResourceRepository fileResourceRepository)
        {
            _fileResourceRepository =
                fileResourceRepository;
        }

        public async Task<AdminFileDetailDto?> Handle(
            GetAdminFileDetailQuery request,
            CancellationToken cancellationToken)
        {
            AdminFileValidation.ValidateActorUserId(
                request.ActorUserId);

            AdminFileValidation.ValidateFileResourceId(
                request.FileResourceId);

            var detail =
                await _fileResourceRepository
                    .GetAdminByIdAsync(
                        request.ActorUserId,
                        request.FileResourceId,
                        cancellationToken);

            return detail is null
                ? null
                : AdminFileDtoMapper
                    .ToDetailDto(detail);
        }
    }

    public sealed class GetAdminFileContentSourceQueryHandler
        : IRequestHandler<
            GetAdminFileContentSourceQuery,
            AdminFileContentSourceDto?>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;

        public GetAdminFileContentSourceQueryHandler(
            IFileResourceRepository fileResourceRepository)
        {
            _fileResourceRepository =
                fileResourceRepository;
        }

        public async Task<AdminFileContentSourceDto?> Handle(
            GetAdminFileContentSourceQuery request,
            CancellationToken cancellationToken)
        {
            AdminFileValidation.ValidateActorUserId(
                request.ActorUserId);

            AdminFileValidation.ValidateFileResourceId(
                request.FileResourceId);

            var detail =
                await _fileResourceRepository
                    .GetAdminByIdAsync(
                        request.ActorUserId,
                        request.FileResourceId,
                        cancellationToken);

            return detail is null
                ? null
                : AdminFileDtoMapper
                    .ToContentSourceDto(detail);
        }
    }
}
