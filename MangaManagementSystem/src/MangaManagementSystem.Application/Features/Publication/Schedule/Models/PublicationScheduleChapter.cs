namespace MangaManagementSystem.Application.Features.Publication.Schedule.Models;

public sealed record PublicationScheduleChapter(
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    string? SeriesCoverUrl,
    Guid ChapterId,
    string ChapterNumberLabel,
    string StatusCode,
    DateTime? PlannedReleaseDate,
    DateTime? ReleasedAtUtc,
    string? PublicationFrequencyCode);
