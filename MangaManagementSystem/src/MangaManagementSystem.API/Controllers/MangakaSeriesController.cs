using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Mangaka.SeriesProposals.Commands.SubmitSeriesProposal;
using MangaManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers
{
    /// <summary>
    /// Thin HTTP boundary for Mangaka series workflows. Controllers only read the request,
    /// resolve the actor, call one Application use case, and map known failures to safe HTTP
    /// responses. No Cloudinary, SQL, repository, or business logic lives here.
    ///
    /// CreateDraftAsync uses the transitional ISeriesService path (to be migrated to MediatR
    /// in a later dedicated task). SubmitProposalAsync introduces the MediatR/CQRS pattern.
    /// </summary>
    [ApiController]
    [Route("api/mangaka/series")]
    public class MangakaSeriesController : ControllerBase
    {
        // Transitional actor header. The API does not yet own authentication; the Web host
        // owns the Blazor cookie/session and forwards the logged-in user's id here. This is a
        // documented temporary server-to-server pattern, not a final auth design.
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly ISeriesService _seriesService;
        private readonly IMediator _mediator;
        private readonly ILogger<MangakaSeriesController> _logger;

        public MangakaSeriesController(
            ISeriesService seriesService,
            IMediator mediator,
            ILogger<MangakaSeriesController> logger)
        {
            _seriesService = seriesService;
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new series draft (status PROPOSAL_DRAFT) with an optional cover image.
        /// Accepts multipart/form-data because the cover file is optional.
        /// Uses transitional ISeriesService path; MediatR migration is a future task.
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

            byte[]? coverBytes = null;
            string? coverFileName = null;
            string? coverContentType = null;

            if (request.CoverFile is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await request.CoverFile.CopyToAsync(ms, cancellationToken);
                coverBytes = ms.ToArray();
                coverFileName = request.CoverFile.FileName;
                coverContentType = request.CoverFile.ContentType;
            }

            var draftDto = new CreateSeriesDraftDto(
                Title: request.Title,
                Synopsis: request.Synopsis,
                Genre: request.Genre,
                ContentLanguageCode: request.ContentLanguageCode,
                Slug: request.Slug,
                PublicationFrequencyCode: request.PublicationFrequencyCode,
                SourceSeriesId: request.SourceSeriesId,
                CoverFileBytes: coverBytes,
                CoverFileName: coverFileName,
                CoverContentType: coverContentType);

            try
            {
                SeriesDraftCreatedDto result = await _seriesService.CreateSeriesDraftAsync(
                    actorUserId, draftDto, cancellationToken);

                return Created($"/api/mangaka/series/{result.SeriesId}", result);
            }
            catch (InvalidOperationException ex)
            {
                // Application/Infrastructure surface friendly, user-safe messages here:
                // only-active-Mangaka, incomplete cover metadata, duplicate slug, invalid code.
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
                ActorUserId: actorUserId,
                SeriesId: seriesId,
                ProposalFileBytes: proposalBytes,
                ProposalFileName: request.ProposalFile.FileName,
                ProposalContentType: request.ProposalFile.ContentType);

            try
            {
                SeriesProposalSubmittedDto result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Application/Infrastructure surface friendly, user-safe messages:
                // not-PROPOSAL_DRAFT, not-active-Mangaka-contributor, series-not-found,
                // file type/size rejection, SHA-256 failure, lock failure.
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting series proposal for series {SeriesId}.", seriesId);
                return Problem(
                    detail: "We could not submit the series proposal right now. Please try again later.",
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
