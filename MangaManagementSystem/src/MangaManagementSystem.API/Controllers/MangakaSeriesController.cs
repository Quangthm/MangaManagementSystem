using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Mangaka.Series.Commands.CancelSeriesDraft;
using MangaManagementSystem.Application.Features.Mangaka.Series.Commands.CreateSeriesDraft;
using MangaManagementSystem.Application.Features.Mangaka.Series.Commands.UpdateSeriesDraft;
using MangaManagementSystem.Application.Features.Mangaka.Series.Queries.GetMyMangakaSeries;
using MangaManagementSystem.Application.Features.Mangaka.Series.Queries.GetMyMangakaSeriesCardById;
using MangaManagementSystem.Application.Features.Mangaka.SeriesProposals.Commands.SubmitSeriesProposal;
using MangaManagementSystem.Application.Features.Mangaka.SeriesProposals.Queries.GetMySeriesProposals;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers
{
    /// <summary>
    /// Thin HTTP boundary for Mangaka series workflows. Controllers only read the request,
    /// resolve the actor, call one Application use case via IMediator, and map known failures
    /// to safe HTTP responses. No Cloudinary, SQL, repository, or business logic lives here.
    ///
    /// All four Mangaka series workflows now use the MediatR/CQRS pattern:
    ///   CreateDraftAsync       → CreateSeriesDraftCommand
    ///   SubmitProposalAsync    → SubmitSeriesProposalCommand
    ///   UpdateDraftProfileAsync → UpdateSeriesDraftCommand
    ///   CancelDraftAsync       → CancelSeriesDraftCommand
    /// </summary>
    [ApiController]
    [Route("api/mangaka/series")]
    public class MangakaSeriesController : ControllerBase
    {
        // Transitional actor header. The API does not yet own authentication; the Web host
        // owns the Blazor cookie/session and forwards the logged-in user's id here. This is a
        // documented temporary server-to-server pattern, not a final auth design.
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IMediator _mediator;
        private readonly ILogger<MangakaSeriesController> _logger;

        public MangakaSeriesController(
            IMediator mediator,
            ILogger<MangakaSeriesController> logger)
        {
            _mediator = mediator;
            _logger   = logger;
        }

        /// <summary>
        /// Creates a new series draft (status PROPOSAL_DRAFT) with an optional cover image.
        /// Accepts multipart/form-data because the cover file is optional.
        /// Uses MediatR/CQRS — all orchestration is in CreateSeriesDraftCommandHandler.
        /// The stored procedure creates the Series, optional SERIES_COVER FileResource,
        /// active SeriesContributor, and audit event.
        /// Must NOT create a SeriesProposal — proposal submission is a separate workflow.
        /// </summary>
        [HttpPost("drafts")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateDraftAsync(
            [FromForm] CreateSeriesDraftForm request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            byte[]? coverBytes       = null;
            string? coverFileName    = null;
            string? coverContentType = null;

            if (request.CoverFile is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await request.CoverFile.CopyToAsync(ms, cancellationToken);
                coverBytes       = ms.ToArray();
                coverFileName    = request.CoverFile.FileName;
                coverContentType = request.CoverFile.ContentType;
            }

            var command = new CreateSeriesDraftCommand(
                ActorUserId:              actorUserId,
                Title:                    request.Title,
                Synopsis:                 request.Synopsis,
                GenreIds:                 request.GenreIds ?? new List<Guid>(),
                TagIds:                   request.TagIds ?? new List<Guid>(),
                ContentLanguageCode:      request.ContentLanguageCode,
                Slug:                     request.Slug,
                PublicationFrequencyCode: request.PublicationFrequencyCode,
                SourceSeriesId:           request.SourceSeriesId,
                CoverFileBytes:           coverBytes,
                CoverFileName:            coverFileName,
                CoverContentType:         coverContentType);

            try
            {
                SeriesDraftCreatedDto result = await _mediator.Send(command, cancellationToken);
                return Created($"/api/mangaka/series/{result.SeriesId}", result);
            }
            catch (InvalidOperationException ex)
            {
                // Handler surfaces friendly messages: invalid actor, title/genre missing,
                // slug derivation failure, cover type/size rejection, SHA-256 failure,
                // duplicate slug, active-Mangaka permission, constraint violations.
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating series draft.");
                return Problem(
                    detail: "We could not create the series draft right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Submits an existing PROPOSAL_DRAFT series for editorial review (BF-SERIES-003).
        /// Requires a proposal document file (PDF/DOC/DOCX, max 10 MB).
        /// Uses MediatR/CQRS — all orchestration is in SubmitSeriesProposalCommandHandler.
        /// The stored procedure creates FileResource, SeriesProposal, transitions Series status,
        /// and writes the audit event. No business logic lives in this controller.
        /// </summary>
        [HttpPost("{seriesId:guid}/proposal-submissions")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitProposalAsync(
            Guid seriesId,
            [FromForm] SubmitSeriesProposalForm request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (request.ProposalFile is not { Length: > 0 })
            {
                return BadRequest(new ApiErrorResponse(
                    "A proposal document file is required."));
            }

            byte[] proposalBytes;
            using (var ms = new MemoryStream())
            {
                await request.ProposalFile.CopyToAsync(ms, cancellationToken);
                proposalBytes = ms.ToArray();
            }

            var command = new SubmitSeriesProposalCommand(
                ActorUserId:        actorUserId,
                SeriesId:           seriesId,
                ProposalFileBytes:  proposalBytes,
                ProposalFileName:   request.ProposalFile.FileName,
                ProposalContentType: request.ProposalFile.ContentType);

            try
            {
                SeriesProposalSubmittedDto result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error submitting series proposal for series {SeriesId}.", seriesId);
                return Problem(
                    detail: "We could not submit the series proposal right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates a PROPOSAL_DRAFT series profile (BF-SERIES-002).
        /// Accepts optional cover image in multipart/form-data.
        /// Cover editing is locked once the series leaves PROPOSAL_DRAFT; the stored procedure
        /// enforces this. Uses MediatR/CQRS — all orchestration is in UpdateSeriesDraftCommandHandler.
        /// </summary>
        [HttpPut("{seriesId:guid}/draft-profile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateDraftProfileAsync(
            Guid seriesId,
            [FromForm] UpdateSeriesDraftForm request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new ApiErrorResponse("A title is required."));
            }

            if (request.GenreIds == null || request.GenreIds.Count == 0 || request.GenreIds.All(id => id == Guid.Empty))
            {
                return BadRequest(new ApiErrorResponse("At least one valid genre is required."));
            }

            if (string.IsNullOrWhiteSpace(request.Synopsis))
            {
                return BadRequest(new ApiErrorResponse("Synopsis / Description is required."));
            }

            byte[]? coverBytes       = null;
            string? coverFileName    = null;
            string? coverContentType = null;

            if (request.CoverFile is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await request.CoverFile.CopyToAsync(ms, cancellationToken);
                coverBytes       = ms.ToArray();
                coverFileName    = request.CoverFile.FileName;
                coverContentType = request.CoverFile.ContentType;
            }

            var command = new UpdateSeriesDraftCommand(
                ActorUserId:              actorUserId,
                SeriesId:                 seriesId,
                Title:                    request.Title,
                Synopsis:                 request.Synopsis,
                GenreIds:                 request.GenreIds ?? new List<Guid>(),
                TagIds:                   request.TagIds ?? new List<Guid>(),
                ContentLanguageCode:      request.ContentLanguageCode,
                PublicationFrequencyCode: request.PublicationFrequencyCode,
                Slug:                     request.Slug,
                CoverFileBytes:           coverBytes,
                CoverFileName:            coverFileName,
                CoverContentType:         coverContentType);

            try
            {
                SeriesDraftUpdatedDto result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error updating series draft profile for series {SeriesId}.", seriesId);
                return Problem(
                    detail: "We could not update the series draft right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Cancels a PROPOSAL_DRAFT series (soft workflow transition to CANCELLED).
        /// Uses MediatR/CQRS — all business logic is in CancelSeriesDraftCommandHandler.
        /// The stored procedure enforces the PROPOSAL_DRAFT guard, contributor permission,
        /// and writes the SERIES_DRAFT_CANCELLED audit event.
        /// Route: POST /api/mangaka/series/{seriesId}/draft-cancellations
        /// (POST rather than DELETE because this is a business state transition, not a physical delete.)
        /// </summary>
        [HttpPost("{seriesId:guid}/draft-cancellations")]
        public async Task<IActionResult> CancelDraftAsync(
            Guid seriesId,
            [FromBody] CancelSeriesDraftRequest? request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            string? reason = string.IsNullOrWhiteSpace(request?.Reason)
                ? null
                : request.Reason.Trim();

            if (reason?.Length > 500)
            {
                return BadRequest(new ApiErrorResponse(
                    "The cancellation reason must be 500 characters or fewer."));
            }

            var command = new CancelSeriesDraftCommand(
                ActorUserId: actorUserId,
                SeriesId:    seriesId,
                Reason:      reason);

            try
            {
                SeriesDraftCancelledDto result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error cancelling series draft {SeriesId}.", seriesId);
                return Problem(
                    detail: "We could not cancel the series draft right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns series where the logged-in actor is an active Mangaka contributor.
        /// Filters by: SeriesContributor.UserId == actorUserId, EndDate IS NULL,
        /// User.StatusCode == "ACTIVE", and Role.RoleName == "Mangaka".
        /// Uses MediatR/CQRS — all orchestration is in GetMyMangakaSeriesQueryHandler.
        /// Route: GET /api/mangaka/series/my-series
        /// </summary>
        [HttpGet("my-series")]
        public async Task<IActionResult> GetMySeriesAsync(
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            var query = new GetMyMangakaSeriesQuery(actorUserId);

            try
            {
                IReadOnlyList<SeriesDto> result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error loading series for actor {ActorUserId}.", actorUserId);
                return Problem(
                    detail: "We could not load your series right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns a single series card by id where the logged-in actor is an active Mangaka contributor.
        /// Same scoping as GET /api/mangaka/series/my-series but targeted to one series.
        /// Returns 404 when the series is not found or the actor is not an active contributor.
        /// Uses MediatR/CQRS — all orchestration is in GetMyMangakaSeriesCardByIdQueryHandler.
        /// Route: GET /api/mangaka/series/{seriesId}/card
        /// </summary>
        [HttpGet("{seriesId:guid}/card")]
        public async Task<IActionResult> GetMySeriesCardByIdAsync(
            Guid seriesId,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            var query = new GetMyMangakaSeriesCardByIdQuery(actorUserId, seriesId);

            try
            {
                var result = await _mediator.Send(query, cancellationToken);
                if (result is null)
                    return NotFound(new ApiErrorResponse("Series not found or you do not have access to it."));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error loading series card {SeriesId} for actor {ActorUserId}.", seriesId, actorUserId);
                return Problem(
                    detail: "We could not load the series card right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns all series proposals scoped to the requesting Mangaka user's active contributor
        /// memberships. Read-only tracking query — no mutations.
        /// Access rule: SeriesContributor.UserId == actorUserId, EndDate IS NULL,
        /// User.StatusCode == "ACTIVE", Role.RoleName == "Mangaka".
        /// Uses MediatR/CQRS — all orchestration is in GetMySeriesProposalsQueryHandler.
        /// Route: GET /api/mangaka/series/proposals
        /// </summary>
        [HttpGet("proposals")]
        public async Task<IActionResult> GetMySeriesProposalsAsync(
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            var query = new GetMySeriesProposalsQuery(actorUserId);

            try
            {
                IReadOnlyList<MangakaSeriesProposalDto> result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error loading proposals for actor {ActorUserId}.", actorUserId);
                return Problem(
                    detail: "We could not load your series proposals right now. Please try again later.",
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
