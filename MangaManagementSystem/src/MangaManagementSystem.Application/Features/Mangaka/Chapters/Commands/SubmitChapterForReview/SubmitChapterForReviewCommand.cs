using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.SubmitChapterForReview
{
    /// <summary>
    /// Command to submit a chapter for editorial review. Sets status to UNDER_REVIEW.
    /// Only allowed when status is DRAFT or REVISION_REQUESTED.
    /// </summary>
    public sealed record SubmitChapterForReviewCommand(
        Guid ActorUserId,
        Guid ChapterId) : IRequest<MangakaChapterListItemDto>;
}
