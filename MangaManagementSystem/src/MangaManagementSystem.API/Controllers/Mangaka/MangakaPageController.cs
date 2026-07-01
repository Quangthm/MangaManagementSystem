using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    /// <summary>
    /// Thin HTTP boundary for Mangaka chapter-page reads and simple edits (list, detail, counts,
    /// page-note update, soft-delete). Page CREATION is part of the page+version+file workflow and is
    /// handled elsewhere. Uses the transitional X-Actor-User-Id header.
    /// </summary>
    [ApiController]
    [Route("api/mangaka/pages")]
    public class MangakaPageController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IChapterPageService _pageService;
        private readonly ILogger<MangakaPageController> _logger;

        public MangakaPageController(IChapterPageService pageService, ILogger<MangakaPageController> logger)
        {
            _pageService = pageService;
            _logger = logger;
        }

        /// <summary>GET /api/mangaka/pages/by-chapter/{chapterId} — non-deleted pages of a chapter.</summary>
        [HttpGet("by-chapter/{chapterId:guid}")]
        public async Task<IActionResult> GetByChapterAsync(Guid chapterId)
        {
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

            if (!TryResolveActorUserId(out _))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

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

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            try
            {
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
