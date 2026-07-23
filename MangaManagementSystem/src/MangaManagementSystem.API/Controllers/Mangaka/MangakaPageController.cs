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
    /// Thin HTTP boundary for Mangaka chapter-page reads and simple edits (list, detail, counts,
    /// page-note update, soft-delete). Page CREATION is part of the page+version+file workflow and is
    /// handled elsewhere. Uses the authenticated JWT Mangaka actor.
    /// </summary>
    [ApiController]
    [Authorize(Roles = MangakaRoleName)]
    [Route("api/mangaka/pages")]
    public class MangakaPageController : ControllerBase
    {
        private const string MangakaRoleName = "Mangaka";

        private readonly IChapterPageService _pageService;
        private readonly IChapterPageVersionService _versionService;
        private readonly IFileResourceService _fileResourceService;
        private readonly IChapterService _chapterService;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly ILogger<MangakaPageController> _logger;

        public MangakaPageController(
            IChapterPageService pageService,
            IChapterPageVersionService versionService,
            IFileResourceService fileResourceService,
            IChapterService chapterService,
            IAuthenticatedActorResolver actorResolver,
            ILogger<MangakaPageController> logger)
        {
            _pageService = pageService;
            _versionService = versionService;
            _fileResourceService = fileResourceService;
            _chapterService = chapterService;
            _actorResolver = actorResolver;
            _logger = logger;
        }


        /// <summary>GET /api/mangaka/pages/by-chapter/{chapterId} — non-deleted pages of a chapter.</summary>
        [HttpGet("by-chapter/{chapterId:guid}")]
        public async Task<IActionResult> GetByChapterAsync(Guid chapterId)
        {
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            if (chapterId == Guid.Empty)
            {
                return BadRequest("Invalid chapter ID.");
            }

            try
            {
                var pages = await _pageService.GetChapterPagesByChapterIdAsync(chapterId);
                return Ok(pages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pages for chapter {ChapterId}.", chapterId);
                return Problem(
                    detail: "Could not load pages right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>GET /api/mangaka/pages/{pageId}</summary>
        [HttpGet("{pageId:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid pageId)
        {
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            if (pageId == Guid.Empty)
            {
                return BadRequest("Invalid page ID.");
            }

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                return page == null ? NotFound() : Ok(page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading page {PageId}.", pageId);
                return Problem(
                    detail: "Could not load the page right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/pages/counts — non-deleted page counts for several chapters.</summary>
        [HttpPost("counts")]
        public async Task<IActionResult> GetCountsAsync([FromBody] PageCountsRequest? request)
        {
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            if (request?.ChapterIds == null)
            {
                return BadRequest("Chapter IDs are required.");
            }

            try
            {
                var counts = await _pageService.GetPageCountsByChapterIdsAsync(request.ChapterIds);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading page counts.");
                return Problem(
                    detail: "Could not load page counts right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>PUT /api/mangaka/pages/{pageId}/notes — update the whole-page note.</summary>
        [HttpPut("{pageId:guid}/notes")]
        public async Task<IActionResult> UpdateNotesAsync(Guid pageId, [FromBody] UpdatePageNotesRequest? request)
        {
            if (pageId == Guid.Empty)
            {
                return BadRequest("Invalid page ID.");
            }

            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                if (page == null)
                {
                    return NotFound();
                }

                var updated = await _pageService.UpdateChapterPageAsync(new UpdateChapterPageDto(
                    ChapterPageId: page.ChapterPageId,
                    ChapterId: page.ChapterId,
                    PageNo: page.PageNo,
                    PageNotes: string.IsNullOrWhiteSpace(request?.PageNotes) ? null : request!.PageNotes.Trim()));

                return updated == null ? NotFound() : Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notes for page {PageId}.", pageId);
                return Problem(
                    detail: "Could not update the page note right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>DELETE /api/mangaka/pages/{pageId} — soft-delete a page.</summary>
        [HttpDelete("{pageId:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid pageId)
        {
            if (pageId == Guid.Empty)
            {
                return BadRequest("Invalid page ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                if (page == null) return NotFound();
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(page.ChapterId);

                var ok = await _pageService.DeleteChapterPageAsync(pageId, actorUserId);
                return ok
                    ? Ok(new { pageId })
                    : BadRequest("The page could not be deleted (it may have assigned tasks or no longer exists).");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page {PageId}.", pageId);
                return Problem(
                    detail: "Could not delete the page right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/pages/create-with-file — atomic page + version 1 + file resource creation.</summary>
        [HttpPost("create-with-file")]
        public async Task<IActionResult> CreatePageWithVersionAsync([FromBody] CreatePageWithVersionRequestDto? request)
        {
            if (request == null) return BadRequest("Request body is required.");
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            try
            {
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(request.ChapterId);
                var result = await _versionService.CreatePageWithVersionAndFileAsync(request, actorUserId, "Mangaka");
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page with version for chapter {ChapterId}.", request.ChapterId);
                return Problem("Could not create page with version. Please try again later.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/pages/versions/by-page-ids — versions for multiple page IDs.</summary>
        [HttpPost("versions/by-page-ids")]
        public async Task<IActionResult> GetVersionsByPageIdsAsync([FromBody] GetVersionsByPageIdsRequest? request)
        {
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            if (request?.PageIds == null) return BadRequest("Page IDs are required.");
            try
            {
                var versions = await _versionService.GetChapterPageVersionsByPageIdsAsync(request.PageIds);
                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading versions by page IDs.");
                return Problem("Could not load versions right now.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>GET /api/mangaka/pages/versions/{versionId}</summary>
        [HttpGet("versions/{versionId:guid}")]
        public async Task<IActionResult> GetVersionByIdAsync(Guid versionId)
        {
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            if (versionId == Guid.Empty) return BadRequest("Invalid version ID.");
            try
            {
                var ver = await _versionService.GetChapterPageVersionByIdAsync(versionId);
                return ver == null ? NotFound() : Ok(ver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading version {VersionId}.", versionId);
                return Problem("Could not load version right now.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/pages/versions/create-with-file — atomic version + file resource + regions.</summary>
        [HttpPost("versions/create-with-file")]
        public async Task<IActionResult> CreateVersionWithFileAndRegionsAsync([FromBody] CreateVersionWithFileAndRegionsRequestDto? request)
        {
            if (request == null) return BadRequest("Request body is required.");
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(request.ChapterPageId);
                if (page == null) return NotFound();
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(page.ChapterId);

                var result = await _versionService.CreateVersionWithFileAndRegionsAsync(
                    request.ChapterPageId,
                    request.VersionNo,
                    request.FileDto,
                    request.VersionNote,
                    request.Regions ?? new List<CreatePageRegionDto>(),
                    request.SetAsCurrent,
                    actorUserId,
                    "Mangaka");
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating version for page {PageId}.", request.ChapterPageId);
                return Problem("Could not create version right now.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>PUT /api/mangaka/pages/versions</summary>
        [HttpPut("versions")]
        public async Task<IActionResult> UpdateVersionAsync([FromBody] UpdateChapterPageVersionDto? request)
        {
            if (request == null) return BadRequest("Request body is required.");
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            try
            {
                var updated = await _versionService.UpdateChapterPageVersionAsync(request);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating version {VersionId}.", request.ChapterPageVersionId);
                return Problem("Could not update version right now.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>PUT /api/mangaka/pages/{pageId}/versions/set-current</summary>
        [HttpPut("{pageId:guid}/versions/set-current")]
        public async Task<IActionResult> SetCurrentVersionAsync(Guid pageId, [FromBody] SetCurrentVersionRequest? request)
        {
            if (pageId == Guid.Empty || request == null) return BadRequest("Invalid request.");
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                if (page == null) return NotFound();
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(page.ChapterId);

                var ok = await _versionService.SetCurrentVersionAsync(pageId, request.ChapterPageVersionId);
                return ok ? Ok() : BadRequest("Could not set current version.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting current version on page {PageId}.", pageId);
                return Problem("Could not set current version right now.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>DELETE /api/mangaka/pages/versions/{versionId}/image</summary>
        [HttpDelete("versions/{versionId:guid}/image")]
        public async Task<IActionResult> DeleteVersionImageAsync(Guid versionId)
        {
            if (versionId == Guid.Empty) return BadRequest("Invalid version ID.");
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            try
            {
                var result = await _versionService.DeleteVersionImageAsync(versionId, actorUserId, "Mangaka");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image for version {VersionId}.", versionId);
                return Problem("Could not delete version image right now.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>POST /api/mangaka/pages/files/by-ids</summary>
        [HttpPost("files/by-ids")]
        public async Task<IActionResult> GetFileResourcesByIdsAsync([FromBody] GetFileResourcesByIdsRequest? request)
        {
            var (_, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null) return actorFailure;

            if (request?.FileIds == null) return BadRequest("File IDs are required.");
            try
            {
                var files = await _fileResourceService.GetFileResourcesByIdsAsync(request.FileIds);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading files by IDs.");
                return Problem("Could not load files right now.", statusCode: StatusCodes.Status500InternalServerError);
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

