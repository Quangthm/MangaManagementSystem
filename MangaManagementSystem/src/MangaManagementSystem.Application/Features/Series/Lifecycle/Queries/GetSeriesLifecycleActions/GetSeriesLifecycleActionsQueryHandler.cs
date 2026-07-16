using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Domain.Policies;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Queries.GetSeriesLifecycleActions
{
    public sealed class GetSeriesLifecycleActionsQueryHandler
        : IRequestHandler<
            GetSeriesLifecycleActionsQuery,
            SeriesLifecycleActionsDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSeriesLifecycleActionsQueryHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesLifecycleActionsDto> Handle(
            GetSeriesLifecycleActionsQuery query,
            CancellationToken cancellationToken)
        {
            if (query.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series must be selected to view lifecycle actions.");
            }

            var series = await _unitOfWork.Series.GetByIdAsync(
                query.SeriesId);

            if (series is null)
            {
                throw new KeyNotFoundException(
                    $"Series '{query.SeriesId:D}' was not found.");
            }

            SeriesLifecycleActorContext actorContext =
                await SeriesLifecycleSupport.ResolveActorContextAsync(
                    _unitOfWork,
                    query.SeriesId,
                    query.ActorUserId,
                    query.ActorRoleName,
                    cancellationToken);

            bool hasLifecycleRole =
                SeriesLifecycleSupport.PauseResumeAllowedRoles.Contains(
                    actorContext.DatabaseRoleName);

            bool isEligibleContributor =
                actorContext.IsActiveContributor
                && hasLifecycleRole;

            bool isMangaka = string.Equals(
                actorContext.DatabaseRoleName,
                SeriesLifecycleSupport.MangakaRoleName,
                StringComparison.OrdinalIgnoreCase);

            return new SeriesLifecycleActionsDto(
                series.SeriesId,
                series.StatusCode,
                CanSetHiatus:
                    isEligibleContributor
                    && SeriesLifecycleTransitionPolicy.CanSetHiatus(
                        series.StatusCode),
                CanResumeSerialization:
                    isEligibleContributor
                    && SeriesLifecycleTransitionPolicy.CanResumeSerialization(
                        series.StatusCode),
                CanCompleteSeries:
                    isEligibleContributor
                    && isMangaka
                    && SeriesLifecycleTransitionPolicy.CanCompleteSeries(
                        series.StatusCode));
        }
    }
}
