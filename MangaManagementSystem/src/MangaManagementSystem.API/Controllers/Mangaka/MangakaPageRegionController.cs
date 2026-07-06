using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    /// <summary>
    /// Thin HTTP boundary for Mangaka page-region reads/writes: create a region, bulk-replace a
    /// version's regions, and read regions/counts for a set of versions. Actor from X-Actor-User-Id
    /// header (used as created-by; the request body's created-by is ignored to prevent spoofing).
    /// </summary>
    [ApiController]
    [Route("api/mangaka/regions")]
    public class MangakaPageRegionController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IPageRegionService _regionService;
        private readonly ILogger<MangakaPageRegionController> _logger;

        public MangakaPageRegionController(IPageRegionService regionService, ILogger<MangakaPageRegionController> logger)
        {
            _regionService = regionService;
            _logger = logger;
        }

        /// <summary>POST /api/mangaka/regions — create a single region on a version.</summary>
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CreatePageRegionDto? dto)
        {
            if (dto == null || dto.ChapterPageVersionId == Guid.Empty)
            {
                return BadRequest("A valid region on a valid version is required.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

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
        public async Task<IActionResult> EnsureFullPageRegionAsync(Guid versionId)
        {
            if (versionId == Guid.Empty)
            {
                return BadRequest("Invalid version ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

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
        public async Task<IActionResult> BulkReplaceAsync(Guid versionId, [FromBody] BulkReplaceRegionsRequest? request)
        {
            if (versionId == Guid.Empty)
            {
                return BadRequest("Invalid version ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

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
        public async Task<IActionResult> GetByVersionsAsync([FromBody] VersionIdsRequest? request)
        {
            if (request?.VersionIds == null)
            {
                return BadRequest("Version IDs are required.");
            }

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
        public async Task<IActionResult> GetCountsAsync([FromBody] VersionIdsRequest? request)
        {
            if (request?.VersionIds == null)
            {
                return BadRequest("Version IDs are required.");
            }

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
