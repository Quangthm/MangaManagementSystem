using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Domain.Policies;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Queries.GetSeriesCompletionImpact
{
    public sealed class GetSeriesCompletionImpactQueryHandler
        : IRequestHandler<
            GetSeriesCompletionImpactQuery,
            SeriesCompletionImpactDto>
    {
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

            if (!SeriesLifecycleTransitionPolicy.CanCompleteSeries(
                    series.StatusCode))
            {
                throw new InvalidOperationException(
                    $"Series '{query.SeriesId:D}' cannot be completed from status " +
                    $"'{series.StatusCode}'.");
            }

            var affectedChapters =
                await _unitOfWork.Chapters.FindAsync(chapter =>
                    chapter.SeriesId == query.SeriesId
                    && SeriesLifecycleSupport
                        .CompletionCancellationStatuses
                        .Contains(chapter.StatusCode));

            var chapterDtos = affectedChapters
                .OrderBy(chapter => chapter.CreatedAtUtc)
                .ThenBy(chapter => chapter.ChapterId)
                .Select(chapter => new SeriesCompletionChapterDto(
                    chapter.ChapterId,
                    chapter.ChapterNumberLabel,
                    chapter.ChapterTitle,
                    chapter.StatusCode))
                .ToList();

            var affectedChapterIds = chapterDtos
                .Select(chapter => chapter.ChapterId)
                .ToHashSet();

            var activeTasks = await _unitOfWork.ChapterPageTasks
                .GetDistinctActiveTasksByChapterIdsAsync(
                    affectedChapterIds,
                    cancellationToken);

            int affectedActiveTaskCount = activeTasks.Count;

            return new SeriesCompletionImpactDto(
                series.SeriesId,
                series.StatusCode,
                chapterDtos.Count,
                affectedActiveTaskCount,
                chapterDtos);
        }
    }
}
