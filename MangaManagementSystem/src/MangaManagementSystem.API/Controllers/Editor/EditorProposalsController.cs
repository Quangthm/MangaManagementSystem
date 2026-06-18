using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.CancelProposalReview;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.ClaimEditorialReview;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.PassProposalToBoard;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.RequestProposalRevision;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Queries.GetEditorProposalDetail;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Queries.GetEditorialProposalQueue;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    /// <summary>
    /// Thin HTTP boundary for Tantou Editor series-proposal review. Controllers only read the
    /// request, resolve the actor, map multipart form data into Application-safe command inputs
    /// (byte[] + name + content type — never IFormFile), call one MediatR use case, and map
    /// known failures to safe HTTP responses. No Cloudinary, SQL, repository, or business logic
    /// lives here.
    /// </summary>
    [ApiController]
    [Route("api/editor/proposals")]
    public class EditorProposalsController : ControllerBase
    {
        // Transitional actor header — same pattern as the Mangaka workflows. The Web host owns
        // the Blazor cookie/session and forwards the logged-in user's id here.
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IMediator _mediator;
        private readonly ILogger<EditorProposalsController> _logger;

        public EditorProposalsController(
            IMediator mediator,
            ILogger<EditorProposalsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Returns the editorial review queue, optionally filtered by proposal status.
        /// Read-only EF query. Route: GET /api/editor/proposals?status=UNDER_EDITORIAL_REVIEW
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetQueueAsync(
            [FromQuery(Name = "status")] string? status,
            CancellationToken cancellationToken)
        {
            var query = new GetEditorialProposalQueueQuery(status);

            try
            {
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading editorial proposal queue.");
                return Problem(
                    detail: "We could not load the proposal queue right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns a single proposal's read-only detail with computed permission flags for the
        /// current actor. Route: GET /api/editor/proposals/{proposalId}
        /// </summary>
        [HttpGet("{proposalId:guid}")]
        public async Task<IActionResult> GetDetailAsync(
            Guid proposalId,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                EditorProposalDetailDto? result =
                    await _mediator.Send(new GetEditorProposalDetailQuery(proposalId, actorUserId), cancellationToken);

                if (result is null)
                {
                    return NotFound(new ApiErrorResponse("The selected proposal could not be found."));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading proposal detail {ProposalId}.", proposalId);
                return Problem(
                    detail: "We could not load the proposal right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Claims a proposal for editorial review. Route: POST /api/editor/proposals/{proposalId}/claims
        /// </summary>
        [HttpPost("{proposalId:guid}/claims")]
        public async Task<IActionResult> ClaimAsync(
            Guid proposalId,
            [FromBody] ClaimProposalRequest? request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            string? notes = string.IsNullOrWhiteSpace(request?.Notes) ? null : request.Notes.Trim();

            var command = new ClaimEditorialReviewCommand(actorUserId, proposalId, notes);

            try
            {
                EditorReviewActionResultDto result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error claiming proposal {ProposalId}.", proposalId);
                return Problem(
                    detail: "We could not claim the proposal right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Records a Request Revision decision. Comments required; markup optional.
        /// Route: POST /api/editor/proposals/{proposalId}/revision-requests
        /// </summary>
        [HttpPost("{proposalId:guid}/revision-requests")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RequestRevisionAsync(
            Guid proposalId,
            [FromForm] RequestRevisionForm request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (string.IsNullOrWhiteSpace(request.Comments))
            {
                return BadRequest(new ApiErrorResponse("Comments are required to request a revision."));
            }

            (byte[]? markupBytes, string? markupName, string? markupContentType) =
                await ReadOptionalFileAsync(request.MarkupFile, cancellationToken);

            var command = new RequestProposalRevisionCommand(
                ActorUserId: actorUserId,
                SeriesProposalId: proposalId,
                Comments: request.Comments,
                MarkupFileBytes: markupBytes,
                MarkupFileName: markupName,
                MarkupContentType: markupContentType);

            return await DispatchDecisionAsync(command, proposalId, "request revision", cancellationToken);
        }

        /// <summary>
        /// Records a Pass To Board decision. Comments and markup optional. Moves to
        /// UNDER_BOARD_REVIEW only. Route: POST /api/editor/proposals/{proposalId}/board-submissions
        /// </summary>
        [HttpPost("{proposalId:guid}/board-submissions")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PassToBoardAsync(
            Guid proposalId,
            [FromForm] PassToBoardForm request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            (byte[]? markupBytes, string? markupName, string? markupContentType) =
                await ReadOptionalFileAsync(request.MarkupFile, cancellationToken);

            var command = new PassProposalToBoardCommand(
                ActorUserId: actorUserId,
                SeriesProposalId: proposalId,
                Comments: request.Comments,
                MarkupFileBytes: markupBytes,
                MarkupFileName: markupName,
                MarkupContentType: markupContentType);

            return await DispatchDecisionAsync(command, proposalId, "pass to board", cancellationToken);
        }

        /// <summary>
        /// Records a Cancel Proposal decision. Comments and markup both required.
        /// Route: POST /api/editor/proposals/{proposalId}/cancellations
        /// </summary>
        [HttpPost("{proposalId:guid}/cancellations")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CancelAsync(
            Guid proposalId,
            [FromForm] CancelProposalForm request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (string.IsNullOrWhiteSpace(request.Comments))
            {
                return BadRequest(new ApiErrorResponse("Comments are required to cancel a proposal."));
            }

            if (request.MarkupFile is not { Length: > 0 })
            {
                return BadRequest(new ApiErrorResponse("A markup file is required to cancel a proposal."));
            }

            (byte[]? markupBytes, string? markupName, string? markupContentType) =
                await ReadOptionalFileAsync(request.MarkupFile, cancellationToken);

            var command = new CancelProposalReviewCommand(
                ActorUserId: actorUserId,
                SeriesProposalId: proposalId,
                Comments: request.Comments,
                MarkupFileBytes: markupBytes!,
                MarkupFileName: markupName!,
                MarkupContentType: markupContentType!);

            return await DispatchDecisionAsync(command, proposalId, "cancel", cancellationToken);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task<IActionResult> DispatchDecisionAsync(
            IRequest<EditorReviewActionResultDto> command,
            Guid proposalId,
            string actionLabel,
            CancellationToken cancellationToken)
        {
            try
            {
                EditorReviewActionResultDto result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error during editorial decision ({Action}) for proposal {ProposalId}.",
                    actionLabel, proposalId);
                return Problem(
                    detail: "We could not complete the editorial action right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<(byte[]? Bytes, string? FileName, string? ContentType)> ReadOptionalFileAsync(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (file is not { Length: > 0 })
            {
                return (null, null, null);
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken);
            return (ms.ToArray(), file.FileName, file.ContentType);
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
