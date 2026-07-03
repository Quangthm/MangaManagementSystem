using System;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PutScheduledChapterOnHold
{
    public sealed record PutScheduledChapterOnHoldCommand(
        Guid ActorUserId,
        Guid ChapterId,
        string Reason) : IRequest<PutScheduledChapterOnHoldResponse>;
}
