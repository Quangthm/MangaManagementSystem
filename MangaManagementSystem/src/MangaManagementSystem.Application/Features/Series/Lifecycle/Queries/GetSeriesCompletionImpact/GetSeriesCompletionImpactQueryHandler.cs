using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Queries.GetSeriesCompletionImpact
{
    public sealed class GetSeriesCompletionImpactQueryHandler
        : IRequestHandler<
            GetSeriesCompletionImpactQuery,
            SeriesCompletionImpactDto>
    {
        private static readonly string[]
            CompletionCancellationStatuses =
            {
                "DRAFT",
                "REVISION_REQUESTED",
                "UNDER_REVIEW",
                "APPROVED",
                "SCHEDULED",
                "ON_HOLD"
            };

        private readonly IUnitOfWork _unitOfWork;

        public GetSeriesCompletionImpactQueryHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesCompletionImpactDto> Handle(
            GetSeriesCompletionImpactQuery query,
            CancellationToken cancellationToken)
        {
            if (query.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series must be selected to preview completion impact.");
            }

            var series = await _unitOfWork.Series.GetByIdAsync(
                query.SeriesId);

            if (series is null)
            {
                throw new KeyNotFoundException(
                    $"Series '{query.SeriesId:D}' was not found.");
            }

            await SeriesLifecycleSupport.ValidateActorAsync(
                _unitOfWork,
                query.SeriesId,
                query.ActorUserId,
                query.ActorRoleName,
                SeriesLifecycleSupport.MangakaOnlyAllowedRoles,
                cancellationToken);

            bool canComplete = string.Equals(
                    series.StatusCode,
                    SeriesLifecycleSupport.SerializedStatusCode,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    series.StatusCode,
                    SeriesLifecycleSupport.HiatusStatusCode,
                    StringComparison.OrdinalIgnoreCase);

            if (!canComplete)
            {
                throw new InvalidOperationException(
                    $"Series '{query.SeriesId:D}' cannot be completed from status " +
                    $"'{series.StatusCode}'.");
            }

            var affectedChapters =
                await _unitOfWork.Chapters.FindAsync(chapter =>
                    chapter.SeriesId == query.SeriesId
                    && CompletionCancellationStatuses.Contains(
                        chapter.StatusCode));

            var chapterDtos = affectedChapters
                .OrderBy(chapter => chapter.CreatedAtUtc)
                .ThenBy(chapter => chapter.ChapterId)
                .Select(chapter => new SeriesCompletionChapterDto(
                    chapter.ChapterId,
                    chapter.ChapterNumberLabel,
                    chapter.ChapterTitle,
                    chapter.StatusCode))
                .ToList();

            return new SeriesCompletionImpactDto(
                series.SeriesId,
                series.StatusCode,
                chapterDtos.Count,
                chapterDtos);
        }
    }
}
