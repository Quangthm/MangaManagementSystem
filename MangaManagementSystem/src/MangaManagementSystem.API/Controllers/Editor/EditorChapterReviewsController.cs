using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewQueue;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    /// <summary>
    /// Thin HTTP boundary for the Tantou Editor Chapter Review Queue. Resolves the actor,
    /// dispatches one MediatR query, returns the result. No business logic, EF, or SQL here.
    /// </summary>
    [ApiController]
    [Route("api/editor/chapters")]
    public class EditorChapterReviewsController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IMediator _mediator;
        private readonly ILogger<EditorChapterReviewsController> _logger;

        public EditorChapterReviewsController(
            IMediator mediator,
            ILogger<EditorChapterReviewsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Returns the chapter review queue read model (KPI counts + filtered chapter list).
        /// Route: GET /api/editor/chapters/review-queue?status=UNDER_REVIEW
        /// </summary>
        [HttpGet("review-queue")]
        public async Task<IActionResult> GetReviewQueueAsync(
            [FromQuery(Name = "status")] string? status,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out _))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                var result = await _mediator.Send(
                    new GetEditorChapterReviewQueueQuery(status), cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading the chapter review queue.");
                return Problem(
                    detail: "We could not load the chapter review queue right now. Please try again later.",
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
