namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record FileUploadResultDto(
        string PublicId,
        string SecureUrl,
        string ContentType,
        long FileSizeBytes,
        string OriginalFileName,
        string? Sha256Hash
    );
}
