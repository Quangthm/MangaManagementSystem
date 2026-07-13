using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Common.Security;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.CompleteSeries;
using MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.ResumeSeriesSerialization;
using MangaManagementSystem.Application.Features.Series.Lifecycle.Commands.SetSeriesHiatus;
using MangaManagementSystem.Application.Features.Series.Lifecycle.Queries.GetSeriesCompletionImpact;
using MangaManagementSystem.Application.Features.Series.Lifecycle.Queries.GetSeriesLifecycleActions;
using MangaManagementSystem.Application.Features.Series.Queries.GetSeriesBySlug;
using MangaManagementSystem.Application.Features.Series.Queries.GetSeriesWorkspaceEntry;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

        /// <summary>
        /// Returns the lifecycle actions currently available to the authenticated actor.
        /// Route: GET /api/series/{seriesId}/lifecycle-actions
        /// </summary>
        [Authorize]
        [HttpGet("{seriesId:guid}/lifecycle-actions")]
        public async Task<IActionResult> GetLifecycleActionsAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveJwtActor(
                    out Guid actorUserId,
                    out string actorRoleName))
            {
                return JwtActorRequired();
            }

            return await ExecuteLifecycleRequestAsync(
                seriesId,
                "loading lifecycle actions",
                () => _mediator.Send(
                    new GetSeriesLifecycleActionsQuery(
                        seriesId,
                        actorUserId,
                        actorRoleName),
                    cancellationToken));
        }

        /// <summary>
        /// Previews the chapters that would be cancelled by completing a series.
        /// Route: GET /api/series/{seriesId}/completion-impact
        /// </summary>
        [Authorize]
        [HttpGet("{seriesId:guid}/completion-impact")]
        public async Task<IActionResult> GetCompletionImpactAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveJwtActor(
                    out Guid actorUserId,
                    out string actorRoleName))
            {
                return JwtActorRequired();
            }

            return await ExecuteLifecycleRequestAsync(
                seriesId,
                "loading completion impact",
                () => _mediator.Send(
                    new GetSeriesCompletionImpactQuery(
                        seriesId,
                        actorUserId,
                        actorRoleName),
                    cancellationToken));
        }

        /// <summary>
        /// Transitions a serialized series to hiatus using a required reason.
        /// Route: POST /api/series/{seriesId}/hiatus
        /// </summary>
        [Authorize]
        [HttpPost("{seriesId:guid}/hiatus")]
        public async Task<IActionResult> SetHiatusAsync(
            Guid seriesId,
            [FromBody] SetSeriesHiatusRequest? request,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveJwtActor(
                    out Guid actorUserId,
                    out string actorRoleName))
            {
                return JwtActorRequired();
            }

            if (request is null)
            {
                return BadRequest(new ApiErrorResponse(
                    "A hiatus request is required."));
            }

            string reason = request.Reason?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(new ApiErrorResponse(
                    "A reason is required to set a series on hiatus."));
            }

            if (reason.Length > 500)
            {
                return BadRequest(new ApiErrorResponse(
                    "The hiatus reason cannot exceed 500 characters."));
            }

            return await ExecuteLifecycleRequestAsync(
                seriesId,
                "setting series hiatus",
                () => _mediator.Send(
                    new SetSeriesHiatusCommand(
                        seriesId,
                        actorUserId,
                        actorRoleName,
                        reason),
                    cancellationToken));
        }

        /// <summary>
        /// Transitions a series on hiatus back to serialization.
        /// Route: POST /api/series/{seriesId}/resume-serialization
        /// </summary>
        [Authorize]
        [HttpPost("{seriesId:guid}/resume-serialization")]
        public async Task<IActionResult> ResumeSerializationAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveJwtActor(
                    out Guid actorUserId,
                    out string actorRoleName))
            {
                return JwtActorRequired();
            }

            return await ExecuteLifecycleRequestAsync(
                seriesId,
                "resuming series serialization",
                () => _mediator.Send(
                    new ResumeSeriesSerializationCommand(
                        seriesId,
                        actorUserId,
                        actorRoleName),
                    cancellationToken));
        }

        /// <summary>
        /// Completes a serialized or hiatus series.
        /// Route: POST /api/series/{seriesId}/complete
        /// </summary>
        [Authorize]
        [HttpPost("{seriesId:guid}/complete")]
        public async Task<IActionResult> CompleteAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveJwtActor(
                    out Guid actorUserId,
                    out string actorRoleName))
            {
                return JwtActorRequired();
            }

            return await ExecuteLifecycleRequestAsync(
                seriesId,
                "completing series",
                () => _mediator.Send(
                    new CompleteSeriesCommand(
                        seriesId,
                        actorUserId,
                        actorRoleName),
                    cancellationToken));
        }

        private async Task<IActionResult> ExecuteLifecycleRequestAsync<TResponse>(
            Guid seriesId,
            string operation,
            Func<Task<TResponse>> sendRequest)
        {
            try
            {
                TResponse result = await sendRequest();
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new ApiErrorResponse(
                        "You do not have permission to perform this series operation."));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiErrorResponse(
                    "The requested series could not be found."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error {Operation} for series {SeriesId}.",
                    operation,
                    seriesId);

                return Problem(
                    detail: "We could not complete this series lifecycle request right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private IActionResult JwtActorRequired()
        {
            return Unauthorized(new ApiErrorResponse(
                "Authenticated actor information is missing or invalid."));
        }

        private bool TryResolveJwtActor(
            out Guid actorUserId,
            out string actorRoleName)
        {
            actorUserId = Guid.Empty;
            actorRoleName = string.Empty;

            if (User.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            string? actorUserIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("user_id")
                ?? User.FindFirstValue("UserId");

            string? resolvedActorRoleName =
                User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role");

            if (!Guid.TryParse(actorUserIdValue, out actorUserId)
                || actorUserId == Guid.Empty
                || string.IsNullOrWhiteSpace(resolvedActorRoleName))
            {
                actorUserId = Guid.Empty;
                return false;
            }

            actorRoleName = resolvedActorRoleName;
            return true;
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
