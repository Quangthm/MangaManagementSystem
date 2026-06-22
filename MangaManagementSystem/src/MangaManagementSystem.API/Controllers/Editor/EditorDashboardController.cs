using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.Dashboard.Queries.GetEditorDashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    /// <summary>
    /// Thin HTTP boundary for the Tantou Editor dashboard read model. Resolves the actor from
    /// the transitional X-Actor-User-Id header (same pattern as the proposal review workflow),
    /// dispatches one MediatR query, and returns the result. No business logic, EF, or SQL here.
    /// </summary>
    [ApiController]
    [Route("api/editor/dashboard")]
    public class EditorDashboardController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IMediator _mediator;
        private readonly ILogger<EditorDashboardController> _logger;

        public EditorDashboardController(
            IMediator mediator,
            ILogger<EditorDashboardController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Returns the editor dashboard read model (KPI counts + proposal queue preview +
        /// recent series activity). Route: GET /api/editor/dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out var actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                EditorDashboardDto result =
                    await _mediator.Send(new GetEditorDashboardQuery(actorUserId), cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading the editor dashboard.");
                return Problem(
                    detail: "We could not load the dashboard right now. Please try again later.",
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
