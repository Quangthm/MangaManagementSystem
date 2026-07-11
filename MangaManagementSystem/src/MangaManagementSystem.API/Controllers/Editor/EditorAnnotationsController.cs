using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.Annotations.Queries.GetEditorAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor;

[ApiController]
[Authorize]
[Route("api/editor/annotations")]
public sealed class EditorAnnotationsController : BaseApiController
{

    private readonly IMediator _mediator;
    private readonly ILogger<EditorAnnotationsController> _logger;

    public EditorAnnotationsController(
        IMediator mediator,
        ILogger<EditorAnnotationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnotationsAsync(
        [FromQuery(Name = "seriesId")] string? seriesId,
        [FromQuery(Name = "issueType")] string? issueType,
        [FromQuery(Name = "status")] string? status,
        CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId();

        try
        {
            var result = await _mediator.Send(
                new GetEditorAnnotationsQuery(seriesId, issueType, status, actorUserId.ToString()),
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading editor annotations.");
            return Problem(
                detail: "We could not load annotations right now. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

}

