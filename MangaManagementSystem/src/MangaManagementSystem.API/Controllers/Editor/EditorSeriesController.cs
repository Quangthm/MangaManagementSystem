using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.Series.Queries.GetEditorSeries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor;

[ApiController]
[Authorize]
[Route("api/editor/series")]
public sealed class EditorSeriesController : BaseApiController
{

    private readonly IMediator _mediator;
    private readonly ILogger<EditorSeriesController> _logger;

    public EditorSeriesController(
        IMediator mediator,
        ILogger<EditorSeriesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSeriesAsync(CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId();

        try
        {
            EditorSeriesListDto result =
                await _mediator.Send(new GetEditorSeriesQuery(actorUserId), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading editor series library.");
            return Problem(
                detail: "We could not load the series library right now. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

}

