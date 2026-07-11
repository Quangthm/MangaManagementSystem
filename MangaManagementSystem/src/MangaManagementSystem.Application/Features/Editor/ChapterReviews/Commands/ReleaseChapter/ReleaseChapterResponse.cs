using System;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.ReleaseChapter
{
    public sealed record ReleaseChapterResponse(
        Guid ChapterId,
        string StatusCode,
        DateTime ReleasedAtUtc,
        DateTime? PlannedReleaseDate,
        string Message);
}
