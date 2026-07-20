using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PutScheduledChapterOnHold;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.ReleaseChapter;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.SetChapterPlannedReleaseDate;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.SubmitChapterEditorialReview;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorActionableChapters;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewDetail;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorChapterReviewQueue;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    /// <summary>
    /// Multipart form request for submitting a chapter editorial review decision with an
    /// optional markup file attachment.
    /// </summary>
    public sealed class SubmitChapterEditorialReviewFormRequest
    {
        public string DecisionCode { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public IFormFile? MarkupFile { get; set; }
    }

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

        /// <summary>
        /// Records a final chapter editorial review decision (APPROVED / REVISION_REQUESTED /
        /// CANCELLED) and updates the chapter status in one transaction.
        /// Route: POST /api/editor/chapters/{chapterId}/review-decision
        /// </summary>
        [HttpPost("{chapterId:guid}/review-decision")]
        public async Task<IActionResult> SubmitReviewDecisionAsync(
            Guid chapterId,
            [FromBody] SubmitChapterEditorialReviewRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (string.IsNullOrWhiteSpace(request.DecisionCode))
            {
                return BadRequest(new ApiErrorResponse("A decision code is required."));
            }

            try
            {
                var command = new SubmitChapterEditorialReviewCommand(
                    actorUserId,
                    chapterId,
                    request.DecisionCode,
                    request.Comments,
                    MarkupFileBytes: null,
                    MarkupFileName: null,
                    MarkupContentType: null);

                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting review decision for chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not process the review decision right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Records a final chapter editorial review decision with an optional markup file
        /// attachment. Route: POST /api/editor/chapters/{chapterId}/review-decision/with-markup
        /// </summary>
        [HttpPost("{chapterId:guid}/review-decision/with-markup")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitReviewDecisionWithMarkupAsync(
            Guid chapterId,
            [FromForm] SubmitChapterEditorialReviewFormRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (string.IsNullOrWhiteSpace(request.DecisionCode))
            {
                return BadRequest(new ApiErrorResponse("A decision code is required."));
            }

            byte[]? markupBytes = null;
            string? markupFileName = null;
            string? markupContentType = null;

            if (request.MarkupFile is { Length: > 0 })
            {
                using var ms = new System.IO.MemoryStream();
                await request.MarkupFile.CopyToAsync(ms, cancellationToken);
                markupBytes = ms.ToArray();
                markupFileName = request.MarkupFile.FileName;
                markupContentType = request.MarkupFile.ContentType;
            }

            try
            {
                var command = new SubmitChapterEditorialReviewCommand(
                    actorUserId,
                    chapterId,
                    request.DecisionCode,
                    request.Comments,
                    markupBytes,
                    markupFileName,
                    markupContentType);

                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting review decision with markup for chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not process the review decision right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{chapterId:guid}/planned-release-date")]
        public async Task<IActionResult> SetPlannedReleaseDateAsync(
            Guid chapterId,
            [FromBody] SetPlannedReleaseDateApiRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));

            try
            {
                var result = await _mediator.Send(
                    new EditorSetChapterPlannedReleaseDateCommand(actorUserId, chapterId, request.PlannedReleaseDate),
                    cancellationToken);

                if (result.PlannedReleaseDate == default)
                    return BadRequest(new ApiErrorResponse(
                        result.ValidationMessage ?? "Planned release date could not be set."));

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error setting planned release date for chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not set the planned release date right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{chapterId:guid}/hold")]
        public async Task<IActionResult> PutChapterOnHoldAsync(
            Guid chapterId,
            [FromBody] EditorPutChapterOnHoldRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));

            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new ApiErrorResponse("A reason is required to put a chapter on hold."));

            try
            {
                var result = await _mediator.Send(
                    new PutScheduledChapterOnHoldCommand(actorUserId, chapterId, request.Reason),
                    cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error putting chapter {ChapterId} on hold.", chapterId);
                return Problem(
                    detail: "We could not put the chapter on hold right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{chapterId:guid}/release")]
        public async Task<IActionResult> ReleaseChapterAsync(
            Guid chapterId,
            [FromBody] EditorReleaseChapterRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));

            try
            {
                var result = await _mediator.Send(
                    new ReleaseChapterCommand(actorUserId, chapterId, request.ConfirmRelease),
                    cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error releasing chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not release the chapter right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("series-chapters")]
        public async Task<IActionResult> GetActionableChaptersAsync(
            [FromQuery(Name = "seriesId")] Guid? seriesId,
            [FromQuery(Name = "searchText")] string? searchText,
            [FromQuery(Name = "statusCode")] string? statusCode,
            [FromQuery(Name = "maxResults")] int? maxResults,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));

            try
            {
                var result = await _mediator.Send(
                    new GetEditorActionableChaptersQuery(
                        actorUserId,
                        seriesId,
                        searchText,
                        statusCode,
                        maxResults ?? 100),
                    cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading editor actionable chapters.");
                return Problem(
                    detail: "We could not load the editor chapter list right now. Please try again later.",
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
