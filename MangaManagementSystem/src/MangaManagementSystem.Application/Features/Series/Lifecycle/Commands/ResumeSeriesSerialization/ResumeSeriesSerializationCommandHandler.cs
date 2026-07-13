using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.ResumeSeriesSerialization
{
    public sealed class ResumeSeriesSerializationCommandHandler
        : IRequestHandler<ResumeSeriesSerializationCommand, SeriesLifecycleChangedDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ResumeSeriesSerializationCommandHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesLifecycleChangedDto> Handle(
            ResumeSeriesSerializationCommand command,
            CancellationToken cancellationToken)
        {
            if (command.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series must be selected to resume serialization.");
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
                        SeriesLifecycleSupport.PauseResumeAllowedRoles,
                        cancellationToken);

                if (series.StatusCode !=
                    SeriesLifecycleSupport.HiatusStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Series '{command.SeriesId:D}' cannot resume serialization from status " +
                        $"'{series.StatusCode}'.");
                }

                DateTime occurredAtUtc = DateTime.UtcNow;

                series.StatusCode =
                    SeriesLifecycleSupport.SerializedStatusCode;
                series.UpdatedAtUtc = occurredAtUtc;
                series.UpdatedByUserId = command.ActorUserId;

                await _unitOfWork.AuditEvents.AddAsync(
                    new AuditEvent
                    {
                        OccurredAtUtc = occurredAtUtc,
                        ActorUserId = command.ActorUserId,
                        ActorRoleName = verifiedRole,
                        ActionCode = "SERIES_SERIALIZATION_RESUMED",
                        EntityType = "Series",
                        EntityId = series.SeriesId.ToString("D"),
                        DetailJson = JsonSerializer.Serialize(new
                        {
                            old_status_code =
                                SeriesLifecycleSupport.HiatusStatusCode,
                            new_status_code =
                                SeriesLifecycleSupport.SerializedStatusCode
                        })
                    },
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new SeriesLifecycleChangedDto(
                    series.SeriesId,
                    SeriesLifecycleSupport.SerializedStatusCode,
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
