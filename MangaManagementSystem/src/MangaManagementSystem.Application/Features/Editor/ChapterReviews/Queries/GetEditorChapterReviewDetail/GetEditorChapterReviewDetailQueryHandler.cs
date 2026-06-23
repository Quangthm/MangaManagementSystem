using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewDetail
{
    /// <summary>
    /// Builds the scoped chapter review detail read model. The repository enforces the access
    /// scope (active Tantou Editor contributor of the chapter's series) and returns null when
    /// the actor is not allowed; this handler simply maps a non-null result to its DTO and
    /// constructs the workspace URL.
    /// </summary>
    public sealed class GetEditorChapterReviewDetailQueryHandler
        : IRequestHandler<GetEditorChapterReviewDetailQuery, EditorChapterReviewDetailDto?>
    {
        private readonly IEditorChapterReviewRepository _repository;

        public GetEditorChapterReviewDetailQueryHandler(IEditorChapterReviewRepository repository)
        {
            _repository = repository;
        }

        public async Task<EditorChapterReviewDetailDto?> Handle(
            GetEditorChapterReviewDetailQuery request, CancellationToken cancellationToken)
        {
            var detail = await _repository.GetReviewDetailForEditorAsync(
                request.ChapterId, request.ActorUserId, cancellationToken);

            // Null means "not found or not authorised" — the controller maps this to 403 so no
            // chapter/series details leak to a non-contributing editor.
            if (detail is null)
            {
                return null;
            }

            var pages = detail.Pages
                .Select(p => new EditorChapterReviewPageDto(
                    p.ChapterPageId,
                    p.PageNumber,
                    p.CurrentVersionId,
                    p.CurrentVersionFileUrl,
                    p.CurrentVersionNo))
                .ToList();

            var annotations = detail.OpenAnnotations
                .Select(a => new EditorChapterReviewAnnotationDto(
                    a.AnnotationId,
                    a.Comment,
                    a.IssueTypeCode,
                    a.CreatedAtUtc,
                    a.CreatedByDisplayName,
                    a.IsResolved))
                .ToList();

            int currentVersionCount = pages.Count(p => p.CurrentVersionId.HasValue);

            string? workspaceUrl = !string.IsNullOrWhiteSpace(detail.SeriesSlug)
                ? $"/series/{detail.SeriesSlug}/workspace?chapterId={detail.ChapterId}&returnUrl={Uri.EscapeDataString($"/editor/chapters/{detail.ChapterId}")}"
                : null;

            return new EditorChapterReviewDetailDto(
                detail.ChapterId,
                detail.SeriesId,
                detail.SeriesTitle,
                detail.SeriesSlug,
                detail.ChapterNumberLabel,
                detail.ChapterTitle,
                detail.StatusCode,
                detail.PageCount,
                currentVersionCount,
                detail.CreatedAtUtc,
                detail.SubmittedByDisplayName,
                // No chapter-to-editor assignment concept exists in the schema (MVP).
                AssignedEditorDisplayName: null,
                pages,
                annotations,
                workspaceUrl,
                CanOpenWorkspace: workspaceUrl is not null);
        }
    }
}
