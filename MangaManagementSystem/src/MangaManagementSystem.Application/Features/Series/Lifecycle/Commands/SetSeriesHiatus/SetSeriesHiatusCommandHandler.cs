using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Domain.Policies;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.SetSeriesHiatus
{
    public sealed class SetSeriesHiatusCommandHandler
        : IRequestHandler<SetSeriesHiatusCommand, SeriesLifecycleChangedDto>
    {
        private const int MaximumReasonLength = 500;

        private readonly IUnitOfWork _unitOfWork;

        public SetSeriesHiatusCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesLifecycleChangedDto> Handle(
            SetSeriesHiatusCommand command,
            CancellationToken cancellationToken)
        {
            if (command.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series must be selected to set hiatus.");
            }

            string reason = command.Reason?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException(
                    "A reason is required to set a series on hiatus.");
            }

            if (reason.Length > MaximumReasonLength)
            {
                throw new InvalidOperationException(
                    "The hiatus reason cannot exceed 500 characters.");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var series = await _unitOfWork.Series.GetByIdAsync(
                    command.SeriesId);

                if (series == null)
                {
                    throw new KeyNotFoundException(
                        $"Series '{command.SeriesId:D}' was not found.");
                }

                string verifiedRoleName =
                    await SeriesLifecycleSupport.ValidateActorAsync(
                        _unitOfWork,
                        command.SeriesId,
                        command.ActorUserId,
                        command.ActorRoleName,
                        SeriesLifecycleSupport.PauseResumeAllowedRoles,
                        cancellationToken);

                if (!SeriesLifecycleTransitionPolicy.CanSetHiatus(
                        series.StatusCode))
                {
                    throw new InvalidOperationException(
                        "Only a serialized series can be set on hiatus.");
                }

                DateTime occurredAtUtc = DateTime.UtcNow;

                series.StatusCode = SeriesLifecycleSupport.HiatusStatusCode;
                series.UpdatedAtUtc = occurredAtUtc;
                series.UpdatedByUserId = command.ActorUserId;

                var auditEvent = new AuditEvent
                {
                    OccurredAtUtc = occurredAtUtc,
                    ActorUserId = command.ActorUserId,
                    ActorRoleName = verifiedRoleName,
                    ActionCode = "SERIES_HIATUS_SET",
                    EntityType = "Series",
                    EntityId = series.SeriesId.ToString("D"),
                    DetailJson = JsonSerializer.Serialize(new
                    {
                        old_status_code =
                            SeriesLifecycleSupport.SerializedStatusCode,
                        new_status_code =
                            SeriesLifecycleSupport.HiatusStatusCode,
                        reason
                    })
                };

                await _unitOfWork.AuditEvents.AddAsync(
                    auditEvent,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new SeriesLifecycleChangedDto(
                    series.SeriesId,
                    SeriesLifecycleSupport.HiatusStatusCode,
                    0);
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
