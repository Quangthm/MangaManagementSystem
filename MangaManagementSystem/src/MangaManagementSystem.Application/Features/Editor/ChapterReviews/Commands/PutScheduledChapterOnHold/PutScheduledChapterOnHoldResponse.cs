using System;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PutScheduledChapterOnHold
{
    public sealed record PutScheduledChapterOnHoldResponse(
        Guid ChapterId,
        string StatusCode,
        string Message);
}
