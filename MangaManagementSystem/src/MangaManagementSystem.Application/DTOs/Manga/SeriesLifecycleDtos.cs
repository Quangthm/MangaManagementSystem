namespace MangaManagementSystem.Application.DTOs.Manga;

public sealed record SeriesLifecycleChangedDto(
    Guid SeriesId,
    string StatusCode,
    int CancelledChapterCount);
