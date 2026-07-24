using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.Series.Queries.GetEditorSeries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    [ApiController]
    [Authorize(Roles = TantouEditorRoleName)]
    [Route("api/editor/series")]
    public class EditorSeriesController : ControllerBase
    {
        private const string TantouEditorRoleName = "Tantou Editor";

        private readonly IMediator _mediator;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly ILogger<EditorSeriesController> _logger;

        public EditorSeriesController(
            IMediator mediator,
            IAuthenticatedActorResolver actorResolver,
            ILogger<EditorSeriesController> logger)
        {
            _mediator = mediator;
            _actorResolver = actorResolver;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetSeriesAsync(CancellationToken cancellationToken)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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
