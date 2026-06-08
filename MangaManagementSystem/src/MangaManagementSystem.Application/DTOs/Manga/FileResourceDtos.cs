using System;
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
        Guid? DeletedByUserId
    );

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
}
