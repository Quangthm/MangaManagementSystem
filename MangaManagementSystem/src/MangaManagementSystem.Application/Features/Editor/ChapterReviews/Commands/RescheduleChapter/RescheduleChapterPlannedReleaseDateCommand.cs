using System;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.RescheduleChapter
{
    public sealed record RescheduleChapterPlannedReleaseDateCommand(
        Guid ActorUserId,
        Guid ChapterId,
        DateTime NewPlannedReleaseDate,
        string Reason) : IRequest<RescheduleChapterResponse>;
}
