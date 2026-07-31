namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record UploadedFileMetadata(
    string OriginalFileName,
    string PublicId,
    string SecureUrl,
    string ContentType,
    long FileSizeBytes,
    string? Sha256Hash);
