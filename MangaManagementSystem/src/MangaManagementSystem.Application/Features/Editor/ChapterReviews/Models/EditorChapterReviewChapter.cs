namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record EditorChapterReviewChapter(
    Guid ChapterId,
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    string ChapterNumberLabel,
    string? ChapterTitle,
    string StatusCode,
    int PageCount,
    DateTime CreatedAtUtc);
