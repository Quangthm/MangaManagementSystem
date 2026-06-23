using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record FileResourceDto(
        Guid FileResourceId,
        string FilePurposeCode,
        string OriginalFileName,
        string CloudinaryPublicId,
        string CloudinarySecureUrl,
        string ContentType,
        long FileSizeBytes,
        string? Sha256Hash,
        Guid? UploadedByUserId,
        DateTime UploadedAtUtc,
        DateTime? DeletedAtUtc,
        Guid? DeletedByUserId,
        DateTime? StorageCleanedAtUtc,
        string? StorageCleanupError
    )
    {
        public bool IsDeleted => DeletedAtUtc.HasValue;

        public string StorageState
        {
            get
            {
                if (!DeletedAtUtc.HasValue)
                {
                    return "Active";
                }

                if (StorageCleanedAtUtc.HasValue)
                {
                    return "Cleaned";
                }

                if (!string.IsNullOrWhiteSpace(StorageCleanupError))
                {
                    return "Failed";
                }

                return "Pending cleanup";
            }
        }
    }

    public record CreateFileResourceDto(
        [Required][MaxLength(50)] string FilePurposeCode,
        [Required][MaxLength(260)] string OriginalFileName,
        [Required][MaxLength(255)] string CloudinaryPublicId,
        [Required][MaxLength(1000)] string CloudinarySecureUrl,
        [Required][MaxLength(100)] string ContentType,
        [Required] long FileSizeBytes,
        [MaxLength(64)] string? Sha256Hash,
        Guid? UploadedByUserId
    );

    public record AdminFileResourceSearchResultDto(
        IReadOnlyList<FileResourceDto> Items,
        int TotalCount,
        int Page,
        int PageSize
    );

    public record FileDeleteResultDto(
        string PublicId,
        string ResourceType,
        string ResultCode,
        bool Deleted,
        bool NotFound
    )
    {
        public bool Completed => Deleted || NotFound;

        public static FileDeleteResultDto DeletedResult(
            string publicId,
            string resourceType,
            string resultCode)
        {
            return new FileDeleteResultDto(
                publicId,
                resourceType,
                resultCode,
                Deleted: true,
                NotFound: false);
        }

        public static FileDeleteResultDto NotFoundResult(
            string publicId,
            string resourceType,
            string resultCode)
        {
            return new FileDeleteResultDto(
                publicId,
                resourceType,
                resultCode,
                Deleted: false,
                NotFound: true);
        }
    }

    public record FileStorageCleanupResultDto(
        Guid FileResourceId,
        string OriginalFileName,
        string StorageState,
        bool Succeeded,
        bool StorageObjectNotFound,
        string Message
    );
}
