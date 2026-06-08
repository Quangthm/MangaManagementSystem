using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/series")]
    public class SeriesController : ControllerBase
    {
        private readonly ISeriesService _seriesService;

        public SeriesController(ISeriesService seriesService)
        {
            _seriesService = seriesService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSeries([FromBody] CreateSeriesDto dto)
        {
            try
            {
                var result = await _seriesService.CreateSeriesAsync(dto);

                return CreatedAtAction(
                    nameof(GetSeriesById),
                    new { id = result.SeriesId },
                    result
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSeries()
        {
            var result = await _seriesService.GetAllSeriesAsync();

            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetSeriesById(long id)
        {
            var result = await _seriesService.GetSeriesByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Series not found." });

            return Ok(result);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateSeries(long id, [FromBody] UpdateSeriesDto dto)
        {
            try
            {
                const int updatedByUserId = 1;

                var result = await _seriesService.UpdateSeriesAsync(id, dto, updatedByUserId);

                if (result == null)
                    return NotFound(new { message = "Series not found." });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteSeries(long id)
        {
            var deleted = await _seriesService.DeleteSeriesAsync(id);

            if (!deleted)
                return NotFound(new { message = "Series not found." });

            return NoContent();
        }
    }
}