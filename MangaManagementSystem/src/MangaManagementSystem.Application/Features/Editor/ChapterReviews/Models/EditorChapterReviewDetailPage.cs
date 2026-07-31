namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record EditorChapterReviewDetailPage(
    Guid ChapterPageId,
    int PageNumber,
    Guid? CurrentVersionId,
    string? CurrentVersionFileUrl,
    short? CurrentVersionNo);
