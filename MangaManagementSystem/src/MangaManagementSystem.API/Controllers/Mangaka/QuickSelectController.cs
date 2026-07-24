using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    [ApiController]
    [Authorize(Roles = MangakaRoleName)]
    [Route("api/mangaka")]
    public class QuickSelectController : ControllerBase
    {
        private const string MangakaRoleName = "Mangaka";

        private readonly IQuickSelectService _quickSelectService;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly ILogger<QuickSelectController> _logger;

        public QuickSelectController(
            IQuickSelectService quickSelectService,
            IAuthenticatedActorResolver actorResolver,
            ILogger<QuickSelectController> logger)
        {
            _quickSelectService = quickSelectService;
            _actorResolver = actorResolver;
            _logger = logger;
        }

        [HttpGet("series/{seriesId:guid}/chapters/quick-select")]
        public async Task<IActionResult> GetQuickSelectChaptersAsync(
            Guid seriesId,
            CancellationToken cancellationToken)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            if (seriesId == Guid.Empty)
            {
                return BadRequest("Invalid series ID.");
            }

            try
            {
                var chapters = await _quickSelectService.GetQuickSelectChaptersAsync(
                    actorUserId, seriesId, cancellationToken);

                return Ok(chapters);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quick-select chapters for series {SeriesId}.", seriesId);
                return Problem(
                    detail: "Could not load chapters right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("chapters/{chapterId:guid}/pages/quick-select")]
        public async Task<IActionResult> GetQuickSelectPagesAsync(
            Guid chapterId,
            CancellationToken cancellationToken)
        {
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            if (chapterId == Guid.Empty)
            {
                return BadRequest("Invalid chapter ID.");
            }

            try
            {
                var pages = await _quickSelectService.GetQuickSelectPagesAsync(
                    chapterId, cancellationToken);

                return Ok(pages);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quick-select pages for chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "Could not load pages right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("series/{seriesId:guid}/assistants/quick-select")]
        public async Task<IActionResult> GetQuickSelectAssistantsAsync(
            Guid seriesId,
            CancellationToken cancellationToken)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            if (seriesId == Guid.Empty)
            {
                return BadRequest("Invalid series ID.");
            }

            try
            {
                var assistants = await _quickSelectService.GetQuickSelectAssistantsAsync(
                    actorUserId, seriesId, cancellationToken);

                return Ok(assistants);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quick-select assistants for series {SeriesId}.", seriesId);
                return Problem(
                    detail: "Could not load assistants right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("tasks/quick-select")]
        public async Task<IActionResult> AssignQuickSelectTasksAsync(
            [FromBody] QuickSelectTaskAssignmentRequest request,
            CancellationToken cancellationToken)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            try
            {
                var result = await _quickSelectService.AssignQuickSelectTasksAsync(
                    actorUserId, request, cancellationToken);


                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quick Select task assignment failed for ActorUserId={ActorUserId}, SeriesId={SeriesId}, ChapterId={ChapterId}, PageCount={PageCount}.",
                    actorUserId, request.SeriesId, request.ChapterId, request.Pages?.Count ?? 0);
                return Problem(
                    detail: "Quick Select assignment failed. No tasks were created.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<(Guid ActorUserId, IActionResult? Failure)> ResolveActorAsync()
        {
            var result = await _actorResolver.ResolveAsync(User, MangakaRoleName);
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

            return result.FailureKind is AuthenticatedActorFailureKind.InvalidIdentity
                or AuthenticatedActorFailureKind.UserNotFound
                ? (Guid.Empty, Unauthorized(response))
                : (Guid.Empty, StatusCode(StatusCodes.Status403Forbidden, response));
        }
    }
}
