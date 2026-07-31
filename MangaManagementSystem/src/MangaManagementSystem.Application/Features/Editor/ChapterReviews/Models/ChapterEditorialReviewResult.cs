namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record ChapterEditorialReviewResult(
    Guid ChapterId,
    string StatusCode,
    Guid ReviewId,
    string DecisionCode,
    string? Comments,
    DateTime ReviewedAtUtc);
