using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CancelChapter;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CancelChapterSubmission;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CreateChapterDraft;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.SetChapterPlannedReleaseDate;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.SubmitChapterForReview;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.UpdateChapterDraft;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Queries.GetMangakaSeriesChapters;
using MangaManagementSystem.Application.Features.Mangaka.Chapters.Queries.GetMyMangakaChapters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = MangakaRoleName)]
    [Route("api/mangaka")]
    public sealed class MangakaChaptersController : ControllerBase
    {
        private const string MangakaRoleName = "Mangaka";

        private readonly IMediator _mediator;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly ILogger<MangakaChaptersController> _logger;

        public MangakaChaptersController(
            IMediator mediator,
            IAuthenticatedActorResolver actorResolver,
            ILogger<MangakaChaptersController> logger)
        {
            _mediator = mediator;
            _actorResolver = actorResolver;
            _logger = logger;
        }

        [HttpGet("chapters")]
        public async Task<IActionResult> GetMyChaptersAsync(CancellationToken cancellationToken)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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

        [HttpPost("chapters/{chapterId:guid}/cancel-submission")]
        public async Task<IActionResult> CancelChapterSubmissionAsync(
            Guid chapterId,
            CancellationToken cancellationToken)
        {
            if (chapterId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid chapter ID."));
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            try
            {
                MangakaChapterListItemDto result = await _mediator.Send(
                    new CancelChapterSubmissionCommand(actorUserId, chapterId), cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error cancelling submission for chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not cancel the submission right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("chapters/{chapterId:guid}/cancel")]
        public async Task<IActionResult> CancelChapterAsync(
            Guid chapterId,
            CancellationToken cancellationToken)
        {
            if (chapterId == Guid.Empty)
            {
                return BadRequest(new ApiErrorResponse("Invalid chapter ID."));
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            try
            {
                MangakaChapterListItemDto result = await _mediator.Send(
                    new CancelChapterCommand(actorUserId, chapterId), cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error cancelling chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "We could not cancel the chapter right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("chapters/{chapterId:guid}/planned-release-date")]
        public async Task<IActionResult> SetPlannedReleaseDateAsync(
            Guid chapterId,
            [FromBody] SetPlannedReleaseDateApiRequest? request,
            CancellationToken cancellationToken)
        {
            if (chapterId == Guid.Empty)
                return BadRequest(new ApiErrorResponse("Invalid chapter ID."));

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            if (request == null)
                return BadRequest(new ApiErrorResponse("Request body is required."));

            try
            {
                var result = await _mediator.Send(
                    new SetChapterPlannedReleaseDateCommand(actorUserId, chapterId, request.PlannedReleaseDate),
                    cancellationToken);

                if (result.PlannedReleaseDate == default)
                {
                    return BadRequest(new ApiErrorResponse(result.ValidationMessage
                        ?? "Planned release date could not be set."));
                }

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

        private async Task<(Guid ActorUserId, IActionResult? Failure)>
            ResolveActorAsync()
        {
            var result = await _actorResolver.ResolveAsync(
                User,
                MangakaRoleName);

            if (result.Succeeded)
            {
                return (result.ActorUserId, null);
            }

            var response = new ApiErrorResponse(
                result.FailureKind == AuthenticatedActorFailureKind.UserNotFound
                    ? "Authenticated Mangaka account was not found."
                    : result.FailureKind == AuthenticatedActorFailureKind.InvalidIdentity
                        ? "Authenticated Mangaka information is invalid."
                        : "The current account is not an active Mangaka.");

            return result.FailureKind is
                AuthenticatedActorFailureKind.InvalidIdentity
                or AuthenticatedActorFailureKind.UserNotFound
                ? (Guid.Empty, Unauthorized(response))
                : (Guid.Empty, StatusCode(
                    StatusCodes.Status403Forbidden,
                    response));
        }
    }
}
