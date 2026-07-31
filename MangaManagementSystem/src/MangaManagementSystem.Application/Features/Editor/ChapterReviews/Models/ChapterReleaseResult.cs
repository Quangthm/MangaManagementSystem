namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record ChapterReleaseResult(
    Guid ChapterId,
    string StatusCode,
    DateTime ReleasedAtUtc,
    DateTime? PlannedReleaseDate,
    string Message);
