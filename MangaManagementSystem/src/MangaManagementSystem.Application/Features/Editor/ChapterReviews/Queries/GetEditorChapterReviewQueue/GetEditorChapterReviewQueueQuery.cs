using System;
using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewQueue
{
    /// <summary>
    /// Read-only query for the Tantou Editor Chapter Review Queue. Returns KPI counts and a
    /// filtered chapter list, scoped to series where the actor is an active Tantou Editor
    /// contributor. Optional status filter narrows the list (e.g. UNDER_REVIEW).
    /// </summary>
    public sealed record GetEditorChapterReviewQueueQuery(
        string? StatusFilter,
        Guid ActorUserId) : IRequest<EditorChapterReviewQueueDto>;
}
