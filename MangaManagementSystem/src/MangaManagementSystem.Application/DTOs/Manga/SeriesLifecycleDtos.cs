namespace MangaManagementSystem.Application.DTOs.Manga;

public sealed record SeriesLifecycleChangedDto(
    Guid SeriesId,
    string StatusCode,
    int CancelledChapterCount);

public sealed record SeriesLifecycleActionsDto(
    Guid SeriesId,
    string StatusCode,
    bool CanSetHiatus,
    bool CanResumeSerialization,
    bool CanCompleteSeries);

public sealed record SeriesCompletionChapterDto(
    Guid ChapterId,
    string ChapterNumberLabel,
    string? ChapterTitle,
    string StatusCode);

public sealed record SeriesCompletionImpactDto(
    Guid SeriesId,
    string SeriesStatusCode,
    int AffectedChapterCount,
    int AffectedActiveTaskCount,
    IReadOnlyList<SeriesCompletionChapterDto> AffectedChapters);
