using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record AssistantTaskSubmitRequestDto(
        [Required] Guid ActorUserId,
        [Required] Guid ChapterPageTaskId,
        [Required][MaxLength(50)] string StorageProviderCode,
        [Required][MaxLength(255)] string PublicId,
        [Required][MaxLength(1000)] string SecureUrl,
        [Required][MaxLength(260)] string OriginalFileName,
        [Required][MaxLength(100)] string ContentType,
        [Required] long FileSizeBytes,
        [Required] string Sha256Hash,
        [MaxLength(500)] string? VersionNote
    );

    public record AssistantTaskSubmitResultDto(
        Guid ChapterPageTaskId,
        Guid FileResourceId,
        Guid CompletedPageVersionId,
        string StatusCode,
        int VersionNo
    );
}
