namespace MangaManagementSystem.Application.DTOs.Manga
{
    public sealed record FileStorageDeleteResultDto(
        bool Success,
        bool NotFound,
        string? Error);
}
