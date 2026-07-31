namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record EditorChapterReviewHistoryItem(
    Guid ReviewId,
    string DecisionCode,
    string? Comments,
    DateTime ReviewedAtUtc,
    Guid ReviewerUserId,
    string ReviewerDisplayName,
    Guid? MarkupFileId,
    string? MarkupFileName,
    string? MarkupFileUrl,
    string? MarkupContentType,
    long? MarkupFileSizeBytes);
