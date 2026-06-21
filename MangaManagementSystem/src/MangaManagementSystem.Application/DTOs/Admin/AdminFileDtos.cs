namespace MangaManagementSystem.Application.DTOs.Admin
{
    public sealed record AdminFileListItemDto(
        Guid FileResourceId,
        string FilePurposeCode,
        string OriginalFileName,
        string ContentType,
        long FileSizeBytes,
        string Sha256Hash,
        Guid? UploadedByUserId,
        string? UploadedByUsername,
        string? UploadedByDisplayName,
        DateTime UploadedAtUtc,
        DateTime? DeletedAtUtc,
        Guid? DeletedByUserId,
        string? DeletedByUsername,
        string? DeletedByDisplayName,
        string StorageCleanupStatus,
        DateTime? StorageCleanedAtUtc,
        Guid? StorageCleanedByUserId,
        string? StorageCleanedByUsername,
        string? StorageCleanedByDisplayName,
        string? StorageCleanupError,
        bool IsDeleted);

    public sealed record AdminFilePageDto(
        IReadOnlyList<AdminFileListItemDto> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages);

    public sealed record AdminFileDetailDto(
        Guid FileResourceId,
        string FilePurposeCode,
        string OriginalFileName,
        string ContentType,
        long FileSizeBytes,
        string Sha256Hash,
        Guid? UploadedByUserId,
        string? UploadedByUsername,
        string? UploadedByDisplayName,
        DateTime UploadedAtUtc,
        DateTime? DeletedAtUtc,
        Guid? DeletedByUserId,
        string? DeletedByUsername,
        string? DeletedByDisplayName,
        string StorageCleanupStatus,
        DateTime? StorageCleanedAtUtc,
        Guid? StorageCleanedByUserId,
        string? StorageCleanedByUsername,
        string? StorageCleanedByDisplayName,
        string? StorageCleanupError,
        long ReferenceCount,
        bool IsDeleted,
        bool CanPreview,
        bool CanDownload);

    public sealed record AdminFileContentSourceDto(
        Guid FileResourceId,
        string OriginalFileName,
        string CloudinarySecureUrl,
        string ContentType,
        long FileSizeBytes,
        DateTime? DeletedAtUtc,
        string StorageCleanupStatus,
        DateTime? StorageCleanedAtUtc,
        bool IsDeleted,
        bool CanPreview);

    public sealed record AdminFileStorageCleanupResultDto(
        Guid FileResourceId,
        string Status,
        string StorageCleanupStatus,
        string Message);

    public sealed record AdminFileStorageCleanupBatchResultDto(
        int TotalCandidates,
        int Cleaned,
        int Missing,
        int Failed,
        int Skipped);
}
