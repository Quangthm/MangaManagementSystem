using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.Dashboard.Queries.GetEditorDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    /// <summary>
    /// Thin HTTP boundary for the Tantou Editor dashboard read model. Resolves the actor from
    /// the authenticated JWT and current account,
    /// dispatches one MediatR query, and returns the result. No business logic, EF, or SQL here.
    /// </summary>
    [ApiController]
    [Authorize(Roles = TantouEditorRoleName)]
    [Route("api/editor/dashboard")]
    public class EditorDashboardController : ControllerBase
    {
        private const string TantouEditorRoleName = "Tantou Editor";

        private readonly IMediator _mediator;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly ILogger<EditorDashboardController> _logger;

        public EditorDashboardController(
            IMediator mediator,
            IAuthenticatedActorResolver actorResolver,
            ILogger<EditorDashboardController> logger)
        {
            _mediator = mediator;
            _actorResolver = actorResolver;
            _logger = logger;
        }

        /// <summary>
        /// Returns the editor dashboard read model (KPI counts + proposal queue preview +
        /// recent series activity). Route: GET /api/editor/dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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

        private async Task<(Guid ActorUserId, IActionResult? Failure)> ResolveActorAsync()
        {
            var result = await _actorResolver.ResolveAsync(User, TantouEditorRoleName);
            if (result.Succeeded)
            {
                return (result.ActorUserId, null);
            }

            var response = new ApiErrorResponse(
                result.FailureKind == AuthenticatedActorFailureKind.UserNotFound
                    ? "Authenticated Tantou Editor account was not found."
                    : result.FailureKind == AuthenticatedActorFailureKind.InvalidIdentity
                        ? "Authenticated Tantou Editor information is invalid."
                        : "The current account is not an active Tantou Editor.");

            return result.FailureKind is AuthenticatedActorFailureKind.InvalidIdentity
                or AuthenticatedActorFailureKind.UserNotFound
                ? (Guid.Empty, Unauthorized(response))
                : (Guid.Empty, StatusCode(StatusCodes.Status403Forbidden, response));
        }
    }
}
