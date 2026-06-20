using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.Annotations.Queries.GetEditorAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    [ApiController]
    [Route("api/editor/annotations")]
    public class EditorAnnotationsController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

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
            if (!TryResolveActorUserId(out var actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

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
