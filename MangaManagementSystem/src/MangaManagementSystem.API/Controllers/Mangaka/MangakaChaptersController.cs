using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CreateChapterDraft;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.ScheduleApprovedChapter;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.SubmitChapterForReview;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.UpdateChapterDraft;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Queries.GetMangakaSeriesChapters;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Queries.GetMyMangakaChapters;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    /// <summary>
    /// Thin HTTP boundary for Mangaka chapter draft and submission management.
    /// Controllers resolve the actor, dispatch one MediatR command/query, and map known
    /// failures to safe HTTP responses. No business logic or persistence lives here.
    /// </summary>
    [ApiController]
    [Route("api/mangaka")]
    public sealed class MangakaChaptersController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IMediator _mediator;
        private readonly ILogger<MangakaChaptersController> _logger;

        public MangakaChaptersController(
            IMediator mediator,
            ILogger<MangakaChaptersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("chapters")]
        public async Task<IActionResult> GetMyChaptersAsync(CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                IReadOnlyList<MangakaChapterListItemDto> result = await _mediator.Send(
                    new GetMyMangakaChaptersQuery(actorUserId), cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading Mangaka chapters for actor {ActorUserId}.", actorUserId);
                return Problem(
                    detail: "We could not load your chapters right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("series/{seriesId:guid}/chapters")]
        public async Task<IActionResult> GetSeriesChaptersAsync(Guid seriesId, CancellationToken cancellationToken)
        {
            if (seriesId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid series ID."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                IReadOnlyList<MangakaChapterListItemDto> result = await _mediator.Send(
                    new GetMangakaSeriesChaptersQuery(actorUserId, seriesId), cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading chapters for series {SeriesId}.", seriesId);
                return Problem(
                    detail: "We could not load the series chapters right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("chapters")]
        public async Task<IActionResult> CreateChapterDraftAsync(
            [FromBody] CreateChapterDraftApiRequest? request,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (request == null)
            {
                return BadRequest(new ApiErrorResponse("Request body is required."));
            }

            try
            {
                var command = new CreateChapterDraftCommand(
                    actorUserId,
                    request.SeriesId,
                    request.ChapterNumberLabel,
                    request.ChapterTitle);

                MangakaChapterListItemDto result = await _mediator.Send(command, cancellationToken);
                return Created($"/api/mangaka/chapters/{result.ChapterId}", result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating chapter draft for actor {ActorUserId}.", actorUserId);
                return Problem(
                    detail: "We could not create the chapter draft right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("chapters/{chapterId:guid}")]
        public async Task<IActionResult> UpdateChapterDraftAsync(
            Guid chapterId,
            [FromBody] UpdateChapterDraftApiRequest? request,
            CancellationToken cancellationToken)
        {
            if (chapterId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid chapter ID."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (request == null)
            {
                return BadRequest(new ApiErrorResponse("Request body is required."));
            }

            try
            {
                var command = new UpdateChapterDraftCommand(
                    actorUserId,
                    chapterId,
                    request.ChapterNumberLabel,
                    request.ChapterTitle);

                MangakaChapterListItemDto result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating chapter draft {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not update the chapter draft right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("chapters/{chapterId:guid}/submit-review")]
        public async Task<IActionResult> SubmitChapterForReviewAsync(
            Guid chapterId,
            CancellationToken cancellationToken)
        {
            if (chapterId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid chapter ID."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                MangakaChapterListItemDto result = await _mediator.Send(
                    new SubmitChapterForReviewCommand(actorUserId, chapterId), cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting chapter {ChapterId} for review.", chapterId);
                return Problem(
                    detail: "We could not submit the chapter for review right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("chapters/{chapterId:guid}/schedule")]
        public async Task<IActionResult> ScheduleApprovedChapterAsync(
            Guid chapterId,
            [FromBody] ScheduleApprovedChapterApiRequest? request,
            CancellationToken cancellationToken)
        {
            if (chapterId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid chapter ID."));
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            if (request == null)
            {
                return BadRequest(new ApiErrorResponse("Request body is required."));
            }

            try
            {
                MangakaChapterListItemDto result = await _mediator.Send(
                    new ScheduleApprovedChapterCommand(actorUserId, chapterId, request.PlannedReleaseDate),
                    cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error scheduling approved chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not schedule the chapter right now. Please try again later.",
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
