using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
    public sealed record AdminFileResourceSearchCriteria(
        Guid ActorUserId,
        string? Search,
        string? FilePurposeCode,
        string DeletedState,
        DateTime? FromUtc,
        DateTime? ToUtc,
        int PageNumber,
        int PageSize);

    public sealed record AdminFileResourceListItem(
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
        string? StorageCleanupError);

    public sealed record AdminFileResourceDetail(
        Guid FileResourceId,
        string FilePurposeCode,
        string OriginalFileName,
        string CloudinaryPublicId,
        string CloudinarySecureUrl,
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
        long ReferenceCount);

    public interface IFileResourceRepository
        : IGenericRepository<FileResource>
    {
        Task<(
            IReadOnlyList<AdminFileResourceListItem> Items,
            int TotalCount)> SearchAdminAsync(
                AdminFileResourceSearchCriteria criteria,
                CancellationToken cancellationToken = default);

        Task<AdminFileResourceDetail?> GetAdminByIdAsync(
            Guid actorUserId,
            Guid fileResourceId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdminFileResourceDetail>>
            GetStorageCleanupCandidatesAsync(
                CancellationToken cancellationToken = default);

        Task SoftDeleteAdminAsync(
            Guid actorUserId,
            Guid fileResourceId,
            string deleteReason,
            CancellationToken cancellationToken = default);

        Task UpdateStorageCleanupResultAsync(
            Guid actorUserId,
            Guid fileResourceId,
            string storageCleanupStatus,
            string? cleanupError,
            string? cleanupReason,
            CancellationToken cancellationToken = default);
    }
}
