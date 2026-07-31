namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record EditorChapterReviewData(
    int UnderReviewCount,
    int ApprovedThisWeekCount,
    int RevisionRequestedCount,
    int OnHoldCount,
    IReadOnlyList<EditorChapterReviewChapter> Chapters);
