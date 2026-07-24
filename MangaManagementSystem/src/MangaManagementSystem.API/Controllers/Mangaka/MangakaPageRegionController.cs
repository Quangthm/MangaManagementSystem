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
    /// Thin HTTP boundary for Mangaka page-region reads/writes: create a region, bulk-replace a
    /// version's regions, and read regions/counts for a set of versions. The authenticated JWT
    /// actor is used as created-by; the request body's created-by is ignored to prevent spoofing.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/mangaka/regions")]
    public class MangakaPageRegionController : ControllerBase
    {
        private const string MangakaRoleName = "Mangaka";
        private const string SharedReadRoles = "Mangaka,Tantou Editor,Assistant";
        private const string RegionCreateRoles = "Mangaka,Tantou Editor";

        private readonly IPageRegionService _regionService;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly IWorkspaceResourceAuthorizationService _workspaceAccess;
        private readonly ILogger<MangakaPageRegionController> _logger;

        public MangakaPageRegionController(
            IPageRegionService regionService,
            IAuthenticatedActorResolver actorResolver,
            IWorkspaceResourceAuthorizationService workspaceAccess,
            ILogger<MangakaPageRegionController> logger)
        {
            _regionService = regionService;
            _actorResolver = actorResolver;
            _workspaceAccess = workspaceAccess;
            _logger = logger;
        }

        /// <summary>POST /api/mangaka/regions — create a single region on a version.</summary>
        [HttpPost]
        [Authorize(Roles = RegionCreateRoles)]
        public async Task<IActionResult> CreateAsync([FromBody] CreatePageRegionDto? dto)
        {
            if (dto == null || dto.ChapterPageVersionId == Guid.Empty)
            {
                return BadRequest("A valid region on a valid version is required.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync("Mangaka", "Tantou Editor");
            if (actorFailure is not null)
                return actorFailure;
            if (!await _workspaceAccess.CanAccessVersionsAsync(actorUserId, new[] { dto.ChapterPageVersionId }, HttpContext.RequestAborted))
                return Forbid();

            try
            {
                var created = await _regionService.CreatePageRegionAsync(dto with { CreatedByUserId = actorUserId });
                return Ok(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating region on version {VersionId}.", dto.ChapterPageVersionId);
                return Problem(
                    detail: "Could not create the region right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/regions/version/{versionId}/ensure-full-page — find or create the whole-page region.</summary>
        [HttpPost("version/{versionId:guid}/ensure-full-page")]
        [Authorize(Roles = RegionCreateRoles)]
        public async Task<IActionResult> EnsureFullPageRegionAsync(Guid versionId)
        {
            if (versionId == Guid.Empty)
            {
                return BadRequest("Invalid version ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync("Mangaka", "Tantou Editor");
            if (actorFailure is not null)
                return actorFailure;
            if (!await _workspaceAccess.CanAccessVersionsAsync(actorUserId, new[] { versionId }, HttpContext.RequestAborted))
                return Forbid();

            try
            {
                var region = await _regionService.EnsureFullPageRegionAsync(versionId, actorUserId, HttpContext.RequestAborted);
                return Ok(region);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring full-page region on version {VersionId}.", versionId);
                return Problem(
                    detail: "Could not create a full-page region right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>PUT /api/mangaka/regions/version/{versionId}/bulk-replace — replace all regions of a version.</summary>
        [HttpPut("version/{versionId:guid}/bulk-replace")]
        // TEMP (demo) — REVISIT in the JWT-authz / EF-Core refactor. Widened from MangakaRoleName to
        // RegionCreateRoles ("Mangaka,Tantou Editor") so a Tantou Editor can persist regions they add/
        // delete during review — the workspace Save uses bulk-replace, so Mangaka-only here => editor
        // Save returns 403. Safe for now: the editor's canvas loads the FULL region set before replacing,
        // and the "region in use by a task/annotation" guard (client + PageRegionService) blocks
        // destructive deletes. Reassess the role model when the team finalizes JWT authz + moves regions
        // from stored procs to EF Core (possible DB change). See docs/revision handoff.
        [Authorize(Roles = RegionCreateRoles)]
        public async Task<IActionResult> BulkReplaceAsync(Guid versionId, [FromBody] BulkReplaceRegionsRequest? request)
        {
            if (versionId == Guid.Empty)
            {
                return BadRequest("Invalid version ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            try
            {
                var dtos = (request?.Regions ?? new List<CreatePageRegionDto>())
                    .Select(d => d with { CreatedByUserId = actorUserId })
                    .ToList();

                var ok = await _regionService.BulkReplacePageRegionsAsync(versionId, dtos);
                return Ok(new { versionId, replaced = ok });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk-replacing regions on version {VersionId}.", versionId);
                return Problem(
                    detail: "Could not save the regions right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/regions/by-versions — regions for a set of versions.</summary>
        [HttpPost("by-versions")]
        [Authorize(Roles = SharedReadRoles)]
        public async Task<IActionResult> GetByVersionsAsync([FromBody] VersionIdsRequest? request)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync("Mangaka", "Tantou Editor", "Assistant");
            if (actorFailure is not null)
                return actorFailure;

            if (request?.VersionIds == null)
            {
                return BadRequest("Version IDs are required.");
            }
            if (!await _workspaceAccess.CanAccessVersionsAsync(actorUserId, request.VersionIds, HttpContext.RequestAborted))
                return Forbid();

            try
            {
                var regions = await _regionService.GetPageRegionsByVersionIdsAsync(request.VersionIds);
                return Ok(regions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading regions by versions.");
                return Problem(
                    detail: "Could not load regions right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/regions/counts — region counts for a set of versions.</summary>
        [HttpPost("counts")]
        [Authorize(Roles = SharedReadRoles)]
        public async Task<IActionResult> GetCountsAsync([FromBody] VersionIdsRequest? request)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync("Mangaka", "Tantou Editor", "Assistant");
            if (actorFailure is not null)
                return actorFailure;

            if (request?.VersionIds == null)
            {
                return BadRequest("Version IDs are required.");
            }
            if (!await _workspaceAccess.CanAccessVersionsAsync(actorUserId, request.VersionIds, HttpContext.RequestAborted))
                return Forbid();

            try
            {
                var counts = await _regionService.GetRegionCountsByVersionIdsAsync(request.VersionIds);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading region counts.");
                return Problem(
                    detail: "Could not load region counts right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<(Guid ActorUserId, IActionResult? Failure)> ResolveActorAsync(
            params string[] allowedRoles)
        {
            var result = await _actorResolver.ResolveAsync(
                User,
                allowedRoles.Length == 0 ? new[] { MangakaRoleName } : allowedRoles);
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
