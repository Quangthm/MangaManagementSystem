using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Domain.Policies;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.CompleteSeries
{
    public sealed class CompleteSeriesCommandHandler
        : IRequestHandler<CompleteSeriesCommand, SeriesLifecycleChangedDto>
    {
        private const string ChapterCancellationReason =
            "Parent series was marked as completed.";

        private static readonly string[] CompletionCancellationStatuses =
        {
            "DRAFT",
            "REVISION_REQUESTED",
            "UNDER_REVIEW",
            "APPROVED",
            "SCHEDULED",
            "ON_HOLD"
        };

        private readonly IUnitOfWork _unitOfWork;

        public CompleteSeriesCommandHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesLifecycleChangedDto> Handle(
            CompleteSeriesCommand command,
            CancellationToken cancellationToken)
        {
            if (command.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series must be selected to complete.");
            }

            if (command.ActorUserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "A valid signed-in user is required to complete a series.");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var series = await _unitOfWork.Series.GetByIdAsync(
                    command.SeriesId);

                if (series is null)
                {
                    throw new KeyNotFoundException(
                        $"Series '{command.SeriesId:D}' was not found.");
                }

                string verifiedRole =
                    await SeriesLifecycleSupport.ValidateActorAsync(
                        _unitOfWork,
                        command.SeriesId,
                        command.ActorUserId,
                        command.ActorRoleName,
                        SeriesLifecycleSupport.MangakaOnlyAllowedRoles,
                        cancellationToken);

                if (!SeriesLifecycleTransitionPolicy.CanCompleteSeries(
                        series.StatusCode))
                {
                    throw new InvalidOperationException(
                        $"Series '{command.SeriesId:D}' cannot be completed from status " +
                        $"'{series.StatusCode}'.");
                }

                var affectedChapters =
                    await _unitOfWork.Chapters.FindAsync(chapter =>
                        chapter.SeriesId == command.SeriesId
                        && CompletionCancellationStatuses.Contains(
                            chapter.StatusCode));

                var chapterTransitions = affectedChapters
                    .Select(chapter => new
                    {
                        Chapter = chapter,
                        OldStatusCode = chapter.StatusCode
                    })
                    .ToArray();

                string oldSeriesStatusCode = series.StatusCode;
                DateTime occurredAtUtc = DateTime.UtcNow;

                series.StatusCode =
                    SeriesLifecycleSupport.CompletedStatusCode;
                series.UpdatedAtUtc = occurredAtUtc;
                series.UpdatedByUserId = command.ActorUserId;

                foreach (var transition in chapterTransitions)
                {
                    transition.Chapter.StatusCode =
                        SeriesLifecycleSupport.CancelledStatusCode;
                    transition.Chapter.UpdatedAtUtc = occurredAtUtc;
                }

                await _unitOfWork.AuditEvents.AddAsync(
                    new AuditEvent
                    {
                        OccurredAtUtc = occurredAtUtc,
                        ActorUserId = command.ActorUserId,
                        ActorRoleName = verifiedRole,
                        ActionCode = "SERIES_COMPLETED",
                        EntityType = "Series",
                        EntityId = series.SeriesId.ToString("D"),
                        DetailJson = JsonSerializer.Serialize(new
                        {
                            old_status_code = oldSeriesStatusCode,
                            new_status_code =
                                SeriesLifecycleSupport.CompletedStatusCode,
                            cancelled_chapter_count =
                                chapterTransitions.Length
                        })
                    },
                    cancellationToken);

                foreach (var transition in chapterTransitions)
                {
                    await _unitOfWork.AuditEvents.AddAsync(
                        new AuditEvent
                        {
                            OccurredAtUtc = occurredAtUtc,
                            ActorUserId = command.ActorUserId,
                            ActorRoleName = verifiedRole,
                            ActionCode =
                                "CHAPTER_CANCELLED_BY_SERIES_COMPLETION",
                            EntityType = "Chapter",
                            EntityId =
                                transition.Chapter.ChapterId.ToString("D"),
                            DetailJson = JsonSerializer.Serialize(new
                            {
                                series_id = series.SeriesId.ToString("D"),
                                old_status_code =
                                    transition.OldStatusCode,
                                new_status_code =
                                    SeriesLifecycleSupport.CancelledStatusCode,
                                reason = ChapterCancellationReason
                            })
                        },
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new SeriesLifecycleChangedDto(
                    series.SeriesId,
                    SeriesLifecycleSupport.CompletedStatusCode,
                    chapterTransitions.Length);
            }
            catch
            {
                try
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        CancellationToken.None);
                }
                catch
                {
                    // Preserve the original handler exception.
                }

                throw;
            }
        }
    }
}
