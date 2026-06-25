using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    [ApiController]
    [Route("api/mangaka")]
    public class QuickSelectController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IQuickSelectService _quickSelectService;
        private readonly ILogger<QuickSelectController> _logger;

        public QuickSelectController(
            IQuickSelectService quickSelectService,
            ILogger<QuickSelectController> logger)
        {
            _quickSelectService = quickSelectService;
            _logger = logger;
        }

        [HttpGet("series/{seriesId:guid}/chapters/quick-select")]
        public async Task<IActionResult> GetQuickSelectChaptersAsync(
            Guid seriesId,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

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
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

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
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

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
