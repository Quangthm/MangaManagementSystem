using System;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.RescheduleChapter
{
    public sealed record RescheduleChapterResponse(
        Guid ChapterId,
        string StatusCode,
        DateTime PlannedReleaseDate,
        string? Message);
}
