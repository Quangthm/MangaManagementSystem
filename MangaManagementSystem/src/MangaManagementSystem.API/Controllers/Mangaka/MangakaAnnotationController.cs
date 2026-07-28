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
    [Authorize]
    [Route("api/mangaka/annotations")]
    public class MangakaAnnotationController : ControllerBase
    {
        private const string MangakaRoleName = "Mangaka";
        private const string AnnotationRoles = "Mangaka,Tantou Editor";

        private readonly IChapterPageAnnotationService _annotationService;
        private readonly IPageRegionService _regionService;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly IWorkspaceResourceAuthorizationService _workspaceAccess;
        private readonly ILogger<MangakaAnnotationController> _logger;

        public MangakaAnnotationController(
            IChapterPageAnnotationService annotationService,
            IPageRegionService regionService,
            IAuthenticatedActorResolver actorResolver,
            IWorkspaceResourceAuthorizationService workspaceAccess,
            ILogger<MangakaAnnotationController> logger)
        {
            _annotationService = annotationService;
            _regionService = regionService;
            _actorResolver = actorResolver;
            _workspaceAccess = workspaceAccess;
            _logger = logger;
        }

        /// <summary>
        /// BR-ANN-013 / BR-ANN-027: annotation mutation from the workspace is permitted for a Tantou Editor
        /// only while the owning chapter is UNDER_REVIEW or REVISION_REQUESTED; in DRAFT, APPROVED,
        /// SCHEDULED, ON_HOLD, RELEASED and CANCELLED their workspace access is read-only. Returns a 403
        /// result when the write must be refused, or null when it may proceed.
        ///
        /// Only the editor is gated here: the rules place no equivalent chapter-state restriction on Mangaka
        /// annotations, so a Mangaka keeps the behaviour they have today. Returns null (allow) when the
        /// chapter cannot be resolved, leaving the existing service/SP permission checks as the authority
        /// rather than blocking a legitimate write on a lookup miss.
        /// </summary>
        private async Task<IActionResult?> EnsureEditorAnnotationStateAllowedAsync(
            Guid anchorRegionId,
            CancellationToken cancellationToken)
        {
            if (!User.IsInRole("Tantou Editor"))
            {
                return null;
            }

            var status = await _regionService.GetChapterStatusByRegionIdAsync(anchorRegionId, cancellationToken);
            if (status is null || status is "UNDER_REVIEW" or "REVISION_REQUESTED")
            {
                return null;
            }

            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(
                $"Editorial annotations cannot be changed while the chapter is {status}."));
        }

        /// <summary>GET /api/mangaka/annotations/by-page/{chapterPageId}</summary>
        [HttpGet("by-page/{chapterPageId:guid}")]
        [Authorize(Roles = AnnotationRoles)]
        public async Task<IActionResult> GetByPageAsync(Guid chapterPageId)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            if (chapterPageId == Guid.Empty)
            {
                return BadRequest("Invalid page ID.");
            }
            if (!await _workspaceAccess.CanAccessPagesAsync(actorUserId, new[] { chapterPageId }, HttpContext.RequestAborted))
                return Forbid();

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
        [Authorize(Roles = AnnotationRoles)]
        public async Task<IActionResult> CreateAsync([FromBody] CreateMangakaAnnotationRequest? request)
        {
            if (request == null || request.PageRegionIds == null || request.PageRegionIds.Count == 0)
            {
                return BadRequest("An annotation must be anchored to at least one page region.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;
            if (!await _workspaceAccess.CanAccessRegionsAsync(actorUserId, request.PageRegionIds, HttpContext.RequestAborted))
                return Forbid();

            // BR-ANN-013 / BR-ANN-027: a Tantou Editor may create an editorial annotation only while the
            // chapter is UNDER_REVIEW or REVISION_REQUESTED; other states are read-only for them. All the
            // regions of one annotation belong to the same page, so the first one settles the chapter.
            var stateFailure = await EnsureEditorAnnotationStateAllowedAsync(
                request.PageRegionIds[0], HttpContext.RequestAborted);
            if (stateFailure is not null)
                return stateFailure;

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
        [Authorize(Roles = AnnotationRoles)]
        public async Task<IActionResult> ResolveAsync(Guid annotationId, [FromBody] ResolveAnnotationRequest? request)
        {
            if (annotationId == Guid.Empty)
            {
                return BadRequest("Invalid annotation ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;
            if (!await _workspaceAccess.CanAccessAnnotationAsync(actorUserId, annotationId, HttpContext.RequestAborted))
                return Forbid();

            // BR-ANN-027: resolving is an editorial-review mutation, so a Tantou Editor may do it only while
            // the chapter is UNDER_REVIEW or REVISION_REQUESTED. The annotation carries its own regions, so
            // resolve the chapter through one of them.
            var annotation = await _annotationService.GetChapterPageAnnotationByIdWithRegionsAsync(annotationId);
            var anchorRegionId = annotation?.PageRegions?.FirstOrDefault()?.PageRegionId;
            if (anchorRegionId.HasValue)
            {
                var stateFailure = await EnsureEditorAnnotationStateAllowedAsync(
                    anchorRegionId.Value, HttpContext.RequestAborted);
                if (stateFailure is not null)
                    return stateFailure;
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

        private async Task<(Guid ActorUserId, IActionResult? Failure)> ResolveActorAsync()
        {
            var result = await _actorResolver.ResolveAsync(User, MangakaRoleName, "Tantou Editor");
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
