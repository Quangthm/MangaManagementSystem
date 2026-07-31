namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record EditorChapterReviewDetail(
    Guid ChapterId,
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    string ChapterNumberLabel,
    string? ChapterTitle,
    string StatusCode,
    int PageCount,
    DateTime CreatedAtUtc,
    string? SubmittedByDisplayName,
    DateTime? PlannedReleaseDate,
    DateTime? ReleasedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<EditorChapterReviewDetailPage> Pages,
    IReadOnlyList<EditorChapterReviewDetailAnnotation> OpenAnnotations,
    IReadOnlyList<EditorChapterReviewHistoryItem> EditorialReviewHistory);
