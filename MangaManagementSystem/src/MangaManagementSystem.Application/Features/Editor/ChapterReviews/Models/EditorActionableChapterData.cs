namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record EditorActionableChapterData(
    Guid ChapterId,
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    string? SeriesCoverUrl,
    string SeriesStatusCode,
    string ChapterNumberLabel,
    string? ChapterTitle,
    string StatusCode,
    DateTime? PlannedReleaseDate,
    DateTime? ReleasedAtUtc,
    string? PublicationFrequencyCode,
    DateTime? UpdatedAtUtc);
