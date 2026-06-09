using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/chapters")]
    public class ChapterController : ControllerBase
    {
        private readonly IChapterService _chapterService;

        public ChapterController(IChapterService chapterService)
        {
            _chapterService = chapterService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateChapter([FromBody] CreateChapterDto dto)
        {
            try
            {
                var result = await _chapterService.CreateChapterAsync(dto);
                return CreatedAtAction(nameof(GetChapterById), new { id = result.ChapterId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChapters()
        {
            var result = await _chapterService.GetAllChaptersAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetChapterById(Guid id)
        {
            var result = await _chapterService.GetChapterByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Chapter not found." });

            return Ok(result);
        }

        [HttpGet("/api/series/{seriesId:guid}/chapters")]
        public async Task<IActionResult> GetChaptersBySeriesId(Guid seriesId)
        {
            var result = await _chapterService.GetChaptersBySeriesIdAsync(seriesId);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] UpdateChapterDto dto)
        {
            try
            {
                var result = await _chapterService.UpdateChapterAsync(id, dto);

                if (result == null)
                    return NotFound(new { message = "Chapter not found." });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteChapter(Guid id)
        {
            var deleted = await _chapterService.DeleteChapterAsync(id);

            if (!deleted)
                return NotFound(new { message = "Chapter not found." });

            return NoContent();
        }
    }
}