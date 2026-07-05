using System;

namespace MangaManagementSystem.Application.DTOs.Editor
{
    public sealed record EditorActionableChapterDto(
        Guid ChapterId,
        Guid SeriesId,
        string SeriesTitle,
        string? SeriesSlug,
        string? SeriesCoverUrl,
        string ChapterNumberLabel,
        string? ChapterTitle,
        string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc,
        string? PublicationFrequencyCode,
        DateTime? UpdatedAtUtc,
        bool CanSchedule,
        bool CanPutOnHold,
        bool CanRelease);
}
