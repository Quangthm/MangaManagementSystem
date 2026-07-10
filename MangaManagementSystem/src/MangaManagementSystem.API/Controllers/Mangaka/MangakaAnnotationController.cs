using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    /// <summary>
    /// Thin HTTP boundary for Mangaka page annotations: create, resolve, and list by page. Author is
    /// the X-Actor-User-Id header actor (BR-ANN-011/013). Uses the transitional actor header.
    /// </summary>
    [ApiController]
    [Route("api/mangaka/annotations")]
    public class MangakaAnnotationController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IChapterPageAnnotationService _annotationService;
        private readonly ILogger<MangakaAnnotationController> _logger;

        public MangakaAnnotationController(IChapterPageAnnotationService annotationService, ILogger<MangakaAnnotationController> logger)
        {
            _annotationService = annotationService;
            _logger = logger;
        }

        /// <summary>GET /api/mangaka/annotations/by-page/{chapterPageId}</summary>
        [HttpGet("by-page/{chapterPageId:guid}")]
        public async Task<IActionResult> GetByPageAsync(Guid chapterPageId)
        {
            if (chapterPageId == Guid.Empty)
            {
                return BadRequest("Invalid page ID.");
            }

            try
            {
                var annotations = await _annotationService.GetChapterPageAnnotationsByChapterPageIdAsync(chapterPageId);
                return Ok(annotations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading annotations for page {ChapterPageId}.", chapterPageId);
                return Problem(
                    detail: "Could not load annotations right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/annotations — create an annotation anchored to page region(s).</summary>
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CreateMangakaAnnotationRequest? request)
        {
            if (request == null || request.PageRegionIds == null || request.PageRegionIds.Count == 0)
            {
                return BadRequest("An annotation must be anchored to at least one page region.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            try
            {
                var created = await _annotationService.CreateChapterPageAnnotationAsync(new CreateChapterPageAnnotationDto(
                    IssueTypeCode: request.IssueTypeCode,
                    AnnotatedByUserId: actorUserId,
                    AnnotationText: request.AnnotationText,
                    PageRegionIds: request.PageRegionIds));

                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogWarning(ex, "SQL error creating annotation.");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating annotation.");
                return Problem(
                    detail: "Could not create the annotation right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/annotations/{annotationId}/resolve</summary>
        [HttpPost("{annotationId:guid}/resolve")]
        public async Task<IActionResult> ResolveAsync(Guid annotationId, [FromBody] ResolveAnnotationRequest? request)
        {
            if (annotationId == Guid.Empty)
            {
                return BadRequest("Invalid annotation ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            try
            {
                var ok = await _annotationService.ResolveAnnotationAsync(actorUserId, annotationId, request?.ResolutionNote);
                return ok ? Ok(new { annotationId, resolved = true }) : BadRequest("The annotation could not be resolved.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving annotation {AnnotationId}.", annotationId);
                return Problem(
                    detail: "Could not resolve the annotation right now. Please try again later.",
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
