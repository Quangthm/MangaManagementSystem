using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Domain.Policies;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorActionableChapters
{
    public sealed class GetEditorActionableChaptersQueryHandler
        : IRequestHandler<GetEditorActionableChaptersQuery, IReadOnlyList<EditorActionableChapterDto>>
    {
        private readonly IEditorChapterReviewRepository _repository;

        public GetEditorActionableChaptersQueryHandler(IEditorChapterReviewRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<EditorActionableChapterDto>> Handle(
            GetEditorActionableChaptersQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                return Array.Empty<EditorActionableChapterDto>();

            if (request.MaxResults < 1)
                return Array.Empty<EditorActionableChapterDto>();

            var clampedMax = Math.Min(request.MaxResults, 200);

            var trimmedSearch = request.SearchText?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSearch))
                trimmedSearch = null;

            var data = await _repository.GetActionableChaptersAsync(
                request.ActorUserId,
                request.SeriesId,
                trimmedSearch,
                request.StatusCode,
                clampedMax,
                cancellationToken);

            var result = new List<EditorActionableChapterDto>(data.Count);
            foreach (var c in data)
            {
                result.Add(Map(c));
            }

            return result.AsReadOnly();
        }

        private static EditorActionableChapterDto Map(EditorActionableChapterData c)
        {
            var canSchedule = c.StatusCode is "DRAFT" or "REVISION_REQUESTED" or "UNDER_REVIEW"
                or "APPROVED" or "SCHEDULED" or "ON_HOLD";
            var canPutOnHold = c.StatusCode == "SCHEDULED";
            var hasReleaseEligibleChapterStatus = c.StatusCode is "SCHEDULED" or "APPROVED";
            var canRelease = hasReleaseEligibleChapterStatus
                && SeriesReleasePolicy.AllowsChapterRelease(c.SeriesStatusCode);

            return new EditorActionableChapterDto(
                c.ChapterId,
                c.SeriesId,
                c.SeriesTitle,
                c.SeriesSlug,
                c.SeriesCoverUrl,
                c.ChapterNumberLabel,
                c.ChapterTitle,
                c.StatusCode,
                c.PlannedReleaseDate,
                c.ReleasedAtUtc,
                c.PublicationFrequencyCode,
                c.UpdatedAtUtc,
                canSchedule,
                canPutOnHold,
                canRelease);
        }
    }
}
