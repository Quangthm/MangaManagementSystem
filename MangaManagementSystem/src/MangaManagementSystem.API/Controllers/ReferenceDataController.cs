using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.ReferenceData.Queries.GetGenres;
using MangaManagementSystem.Application.Features.ReferenceData.Queries.GetTags;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/reference")]
    public class ReferenceDataController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ReferenceDataController> _logger;

        public ReferenceDataController(IMediator mediator, ILogger<ReferenceDataController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("genres")]
        public async Task<IActionResult> GetGenresAsync(CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<GenreDto> genres = await _mediator.Send(new GetGenresQuery(), cancellationToken);
                return Ok(genres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading reference genres.");
                return Problem(
                    detail: "We could not load the genre list right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("tags")]
        public async Task<IActionResult> GetTagsAsync(CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<TagDto> tags = await _mediator.Send(new GetTagsQuery(), cancellationToken);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading reference tags.");
                return Problem(
                    detail: "We could not load the tag list right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
