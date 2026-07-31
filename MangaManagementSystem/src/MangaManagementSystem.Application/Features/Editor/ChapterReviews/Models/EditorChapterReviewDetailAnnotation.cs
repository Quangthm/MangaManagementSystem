namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record EditorChapterReviewDetailAnnotation(
    Guid AnnotationId,
    string Comment,
    string IssueTypeCode,
    DateTime CreatedAtUtc,
    string? CreatedByDisplayName,
    bool IsResolved,
    int? PageNumber,
    Guid? CurrentVersionId,
    short? CurrentVersionNo);
