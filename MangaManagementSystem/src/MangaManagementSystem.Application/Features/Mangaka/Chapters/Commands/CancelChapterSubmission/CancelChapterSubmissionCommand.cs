using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CancelChapterSubmission
{
    /// <summary>
    /// Command to cancel a pending submission, returning the chapter to DRAFT so it becomes editable
    /// again. Only allowed when the chapter is UNDER_REVIEW.
    /// </summary>
    public sealed record CancelChapterSubmissionCommand(
        Guid ActorUserId,
        Guid ChapterId) : IRequest<MangakaChapterListItemDto>;
}
