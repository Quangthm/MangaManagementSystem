using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Features.Publication.Schedule.Queries.GetPublicationScheduleCalendar;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Publication
{
    [ApiController]
    [Route("api/publication/schedule")]
    public class PublicationScheduleController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IPublicationScheduleRepository _scheduleRepository;
        private readonly ILogger<PublicationScheduleController> _logger;

        public PublicationScheduleController(
            IMediator mediator,
            IPublicationScheduleRepository scheduleRepository,
            ILogger<PublicationScheduleController> logger)
        {
            _mediator = mediator;
            _scheduleRepository = scheduleRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduleAsync(
            [FromQuery] DateTime? anchorDate,
            [FromQuery] Guid? seriesId,
            [FromQuery] string? frequencyCode,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(
                    new GetPublicationScheduleCalendarQuery(anchorDate, seriesId, frequencyCode),
                    cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading publication schedule.");
                return Problem(
                    detail: "We could not load the publication schedule right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("series-suggestions")]
        public async Task<IActionResult> GetSeriesSuggestionsAsync(
            [FromQuery] string searchText,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    return Ok(Array.Empty<PublicationScheduleSeriesSuggestion>());
                }

                var result = await _scheduleRepository.GetSeriesSuggestionsAsync(
                    searchText, maxResults: 10, ct: cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading series suggestions.");
                return Problem(
                    detail: "We could not load series suggestions right now.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
