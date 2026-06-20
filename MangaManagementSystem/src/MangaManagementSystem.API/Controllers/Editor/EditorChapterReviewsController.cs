using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewDetail;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewQueue;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    /// <summary>
    /// Thin HTTP boundary for the Tantou Editor Chapter Review queue and detail. Resolves the
    /// actor, dispatches one MediatR query, returns the result. No business logic, EF, or SQL
    /// here. Both endpoints are scoped to series the actor contributes to.
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
        /// Returns the chapter review queue read model (KPI counts + filtered chapter list),
        /// scoped to the actor's series.
        /// Route: GET /api/editor/chapters/review-queue?status=UNDER_REVIEW
        /// </summary>
        [HttpGet("review-queue")]
        public async Task<IActionResult> GetReviewQueueAsync(
            [FromQuery(Name = "status")] string? status,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                var result = await _mediator.Send(
                    new GetEditorChapterReviewQueueQuery(status, actorUserId), cancellationToken);
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

        /// <summary>
        /// Returns the scoped review detail for one chapter. Responds 403 when the actor is not
        /// an active Tantou Editor contributor of the chapter's series (no details leaked).
        /// Route: GET /api/editor/chapters/{chapterId}/review-detail
        /// </summary>
        [HttpGet("{chapterId:guid}/review-detail")]
        public async Task<IActionResult> GetReviewDetailAsync(
            Guid chapterId,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                EditorChapterReviewDetailDto? result = await _mediator.Send(
                    new GetEditorChapterReviewDetailQuery(chapterId, actorUserId), cancellationToken);

                if (result is null)
                {
                    // Not found OR not authorised — same safe response, no details leaked.
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(
                        "You do not have access to this chapter review."));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading chapter review detail {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not load the chapter review right now. Please try again later.",
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
