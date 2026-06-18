using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Commands.CancelSeriesDraft
{
    /// <summary>
    /// Handles the Cancel Draft workflow.
    ///
    /// Orchestration:
    ///   1. Validate command inputs.
    ///   2. Call manga.usp_Series_CancelDraft through ISeriesRepository.
    ///   3. Return SeriesDraftCancelledDto on success.
    ///
    /// No Cloudinary upload or cleanup is required — cancellation is a pure status
    /// transition. The stored procedure owns transaction, status guard, contributor
    /// permission check, and audit.
    /// </summary>
    public sealed class CancelSeriesDraftCommandHandler
        : IRequestHandler<CancelSeriesDraftCommand, SeriesDraftCancelledDto>
    {
        private readonly ISeriesRepository _seriesRepository;
        private readonly ILogger<CancelSeriesDraftCommandHandler> _logger;

        public CancelSeriesDraftCommandHandler(
            ISeriesRepository seriesRepository,
            ILogger<CancelSeriesDraftCommandHandler> logger)
        {
            _seriesRepository = seriesRepository;
            _logger = logger;
        }

        public async Task<SeriesDraftCancelledDto> Handle(
            CancelSeriesDraftCommand command,
            CancellationToken cancellationToken)
        {
            if (command.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in user is required to cancel a series draft.");
            }

            if (command.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series must be selected to cancel.");
            }

            string? reason = string.IsNullOrWhiteSpace(command.Reason)
                ? null
                : command.Reason.Trim();

            try
            {
                await _seriesRepository.CancelSeriesDraftViaProcAsync(
                    actorUserId: command.ActorUserId,
                    seriesId: command.SeriesId,
                    reason: reason,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to cancel series draft {SeriesId} by actor {ActorUserId}.",
                    command.SeriesId, command.ActorUserId);
                throw;
            }

            return new SeriesDraftCancelledDto
            {
                SeriesId = command.SeriesId,
                StatusCode = "CANCELLED"
            };
        }
    }
}
