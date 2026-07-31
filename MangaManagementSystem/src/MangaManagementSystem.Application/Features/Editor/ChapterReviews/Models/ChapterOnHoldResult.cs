namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

public sealed record ChapterOnHoldResult(
    Guid ChapterId,
    string StatusCode,
    string Message);
