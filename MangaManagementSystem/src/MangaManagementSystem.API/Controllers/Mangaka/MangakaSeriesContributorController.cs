using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Mangaka.Contributors.Commands.AddAssistantContributor;
using MangaManagementSystem.Application.Features.Mangaka.Contributors.Commands.EndAssistantContributor;
using MangaManagementSystem.Application.Features.Mangaka.Contributors.Queries.GetSeriesContributors;
using MangaManagementSystem.Application.Features.Mangaka.Contributors.Queries.SearchEligibleAssistants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    [ApiController]
    [Route("api/mangaka/series/{seriesId:guid}/contributors")]
    public sealed class MangakaSeriesContributorController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IMediator _mediator;

        public MangakaSeriesContributorController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns all contributor rows (active and former) for the specified series
        /// where the actor is an active Mangaka contributor.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetContributorsAsync(
            Guid seriesId,
            CancellationToken cancellationToken)
        {
            if (seriesId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid series ID."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse("Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                var query = new GetSeriesContributorsQuery(actorUserId, seriesId);
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception)
            {
                return Problem(
                    detail: "Could not load contributors right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns active Assistant users who are eligible to be added as contributors
        /// to the specified series. Excludes users who are already active contributors.
        /// </summary>
        [HttpGet("eligible-assistants")]
        public async Task<IActionResult> SearchEligibleAssistantsAsync(
            Guid seriesId,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            if (seriesId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid series ID."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse("Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                var query = new SearchEligibleAssistantsQuery(actorUserId, seriesId, search);
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception)
            {
                return Problem(
                    detail: "Could not search eligible assistants right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Adds an active Assistant user as a contributor to the specified series.
        /// Requires the actor to be an active Mangaka contributor of that series.
        /// </summary>
        [HttpPost("assistants")]
        public async Task<IActionResult> AddAssistantAsync(
            Guid seriesId,
            [FromBody] AddAssistantContributorRequest? request,
            CancellationToken cancellationToken)
        {
            if (seriesId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid series ID."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse("Could not identify the requesting user. Please sign in again."));
            }

            if (request == null || request.AssistantUserId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("A valid assistant user must be selected."));
            }

            try
            {
                var command = new AddAssistantContributorCommand(actorUserId, seriesId, request.AssistantUserId);
                await _mediator.Send(command, cancellationToken);
                return Ok(new { seriesId, assistantUserId = request.AssistantUserId, added = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception)
            {
                return Problem(
                    detail: "Could not add the assistant contributor right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Ends an active Assistant contributor's membership in the specified series
        /// by setting their end_date. Requires the actor to be an active Mangaka contributor.
        /// </summary>
        [HttpPost("assistants/{assistantUserId:guid}/end")]
        public async Task<IActionResult> EndAssistantAsync(
            Guid seriesId,
            Guid assistantUserId,
            [FromBody] EndAssistantContributorRequest? request,
            CancellationToken cancellationToken)
        {
            if (seriesId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid series ID."));
            }

            if (assistantUserId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid assistant user ID."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse("Could not identify the requesting user. Please sign in again."));
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new ApiErrorResponse("A reason is required to remove an assistant contributor."));
            }

            try
            {
                var command = new EndAssistantContributorCommand(actorUserId, seriesId, assistantUserId, request.Reason);
                await _mediator.Send(command, cancellationToken);
                return Ok(new { seriesId, assistantUserId, ended = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception)
            {
                return Problem(
                    detail: "Could not remove the assistant contributor right now. Please try again later.",
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
