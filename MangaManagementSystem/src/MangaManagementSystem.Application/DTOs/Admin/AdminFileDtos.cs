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
        bool IsDeleted,
        bool CanPreview);
}
