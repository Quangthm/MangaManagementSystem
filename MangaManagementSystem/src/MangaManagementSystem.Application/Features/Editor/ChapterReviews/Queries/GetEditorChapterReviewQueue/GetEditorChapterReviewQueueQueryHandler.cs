using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewQueue
{
    /// <summary>
    /// Builds the chapter review queue read model from the repository and maps Domain records
    /// to API-facing DTOs. Workspace URLs are constructed here so the UI can navigate directly
    /// to <c>/series/{slug}/workspace?chapterId={id}</c>.
    /// </summary>
    public sealed class GetEditorChapterReviewQueueQueryHandler
        : IRequestHandler<GetEditorChapterReviewQueueQuery, EditorChapterReviewQueueDto>
    {
        private readonly IEditorChapterReviewRepository _repository;

        public GetEditorChapterReviewQueueQueryHandler(IEditorChapterReviewRepository repository)
        {
            _repository = repository;
        }

        public async Task<EditorChapterReviewQueueDto> Handle(
            GetEditorChapterReviewQueueQuery request, CancellationToken cancellationToken)
        {
            var data = await _repository.GetReviewQueueAsync(
                request.StatusFilter, request.ActorUserId, cancellationToken);

            var chapters = data.Chapters
                .Select(c =>
                {
                    string? slug = c.Series?.Slug;
                    string? workspaceUrl = !string.IsNullOrWhiteSpace(slug)
                        ? $"/series/{slug}/workspace?chapterId={c.ChapterId}&returnUrl={Uri.EscapeDataString("/editor/chapters")}"
                        : null;

                    return new EditorChapterReviewQueueItemDto(
                        c.ChapterId,
                        c.SeriesId,
                        c.Series?.Title ?? string.Empty,
                        slug,
                        c.ChapterNumberLabel,
                        c.ChapterTitle,
                        c.StatusCode,
                        c.PageCount,
                        c.CreatedAtUtc,
                        workspaceUrl);
                })
                .ToList();

            return new EditorChapterReviewQueueDto(
                data.UnderReviewCount,
                data.ApprovedThisWeekCount,
                data.RevisionRequestedCount,
                data.OnHoldCount,
                chapters);
        }
    }
}
