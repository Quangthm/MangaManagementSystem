namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record EditorChapterReviewChapter(
    Guid ChapterId,
    Guid SeriesId,
    string ChapterNumberLabel,
    string? ChapterTitle,
    string StatusCode,
    int PageCount,
    DateTime CreatedAtUtc,
    MangaManagementSystem.Domain.Entities.Series? Series);
