using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Common.Security;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Series.Queries.GetSeriesBySlug;
using MangaManagementSystem.Application.Features.Series.Queries.GetSeriesWorkspaceEntry;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers
{
    /// <summary>
    /// Thin HTTP boundary for general (non-Mangaka-specific) series read access.
    /// Serves the /series/{slug} detail page and the workspace-entry access check.
    /// Controllers only read the request, resolve the actor, call one Application use
    /// case via IMediator, and map known failures to safe HTTP responses.
    /// </summary>
    [ApiController]
    [Route("api/series")]
    public class SeriesController : ControllerBase
    {
        // Transitional actor header. The API does not yet own authentication; the Web host
        // owns the Blazor cookie/session and forwards the logged-in user's id here.
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IMediator _mediator;
        private readonly ILogger<SeriesController> _logger;

        public SeriesController(IMediator mediator, ILogger<SeriesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Returns series detail by slug with active contributors and a paginated chapter list.
        /// Route: GET /api/series/{slug}?chapterPage=1&amp;chapterPageSize=10
        /// </summary>
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlugAsync(
            string slug,
            [FromQuery] int chapterPage = 1,
            [FromQuery] int chapterPageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest(new ApiErrorResponse("A series slug is required."));
            }

            var query = new GetSeriesBySlugQuery(slug, chapterPage, chapterPageSize);

            try
            {
                SeriesDetailDto? result = await _mediator.Send(query, cancellationToken);
                if (result is null)
                {
                    return NotFound(new ApiErrorResponse("The requested series could not be found."));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading series detail for slug {Slug}.", LogSanitizer.Sanitize(slug));
                return Problem(
                    detail: "We could not load this series right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the workspace entry access decision for the current actor and series slug.
        /// Used by the series page (to enable/disable Open Workspace) and the workspace page
        /// (to enforce series-specific access before loading workspace content).
        /// Route: GET /api/series/{slug}/workspace-entry
        /// </summary>
        [HttpGet("{slug}/workspace-entry")]
        public async Task<IActionResult> GetWorkspaceEntryAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest(new ApiErrorResponse("A series slug is required."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            var query = new GetSeriesWorkspaceEntryQuery(slug, actorUserId);

            try
            {
                SeriesWorkspaceEntryDto? result = await _mediator.Send(query, cancellationToken);
                if (result is null)
                {
                    return NotFound(new ApiErrorResponse("The requested series could not be found."));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error checking workspace access for slug {Slug}.", LogSanitizer.Sanitize(slug));
                return Problem(
                    detail: "We could not verify workspace access right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private bool TryResolveActorUserId(out Guid actorUserId)
        {
            actorUserId = Guid.Empty;

            if (Request.Headers.TryGetValue(ActorUserIdHeader, out var headerValues))
            {
                string? raw = headerValues.ToString();
                if (Guid.TryParse(raw, out actorUserId) && actorUserId != Guid.Empty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
