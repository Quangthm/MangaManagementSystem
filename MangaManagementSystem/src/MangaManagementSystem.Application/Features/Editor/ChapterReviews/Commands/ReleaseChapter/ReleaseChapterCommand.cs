using System;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.ReleaseChapter
{
    public sealed record ReleaseChapterCommand(
        Guid ActorUserId,
        Guid ChapterId,
        bool ConfirmRelease) : IRequest<ReleaseChapterResponse>;
}
