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
    /// <summary>
    /// Thin HTTP boundary for Mangaka page annotations: create, resolve, and list by page. Author is
    /// the authenticated JWT actor (BR-ANN-011/013).
    /// </summary>
    [ApiController]
    [Authorize(Roles = MangakaRoleName)]
    [Route("api/mangaka/annotations")]
    public class MangakaAnnotationController : ControllerBase
    {
        private const string MangakaRoleName = "Mangaka";

        private readonly IChapterPageAnnotationService _annotationService;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly ILogger<MangakaAnnotationController> _logger;

        public MangakaAnnotationController(
            IChapterPageAnnotationService annotationService,
            IAuthenticatedActorResolver actorResolver,
            ILogger<MangakaAnnotationController> logger)
        {
            _annotationService = annotationService;
            _actorResolver = actorResolver;
            _logger = logger;
        }

        /// <summary>GET /api/mangaka/annotations/by-page/{chapterPageId}</summary>
        [HttpGet("by-page/{chapterPageId:guid}")]
        public async Task<IActionResult> GetByPageAsync(Guid chapterPageId)
        {
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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
