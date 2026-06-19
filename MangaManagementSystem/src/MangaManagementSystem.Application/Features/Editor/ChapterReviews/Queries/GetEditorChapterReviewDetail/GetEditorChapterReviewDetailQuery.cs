using System;
using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewDetail
{
    /// <summary>
    /// Read-only, access-scoped query for a single chapter's review detail. Returns null when
    /// the chapter does not exist or the actor is not an active Tantou Editor contributor of
    /// the chapter's series, so the API can respond 403 without leaking details.
    /// </summary>
    public sealed record GetEditorChapterReviewDetailQuery(
        Guid ChapterId,
        Guid ActorUserId) : IRequest<EditorChapterReviewDetailDto?>;
}
