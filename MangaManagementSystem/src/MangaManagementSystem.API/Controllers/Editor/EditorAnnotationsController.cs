using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Features.Editor.Annotations.Queries.GetEditorAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Editor
{
    [ApiController]
    [Authorize(Roles = TantouEditorRoleName)]
    [Route("api/editor/annotations")]
    public class EditorAnnotationsController : ControllerBase
    {
        private const string TantouEditorRoleName = "Tantou Editor";

        private readonly IMediator _mediator;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly ILogger<EditorAnnotationsController> _logger;

        public EditorAnnotationsController(
            IMediator mediator,
            IAuthenticatedActorResolver actorResolver,
            ILogger<EditorAnnotationsController> logger)
        {
            _mediator = mediator;
            _actorResolver = actorResolver;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnotationsAsync(
            [FromQuery(Name = "seriesId")] string? seriesId,
            [FromQuery(Name = "issueType")] string? issueType,
            [FromQuery(Name = "status")] string? status,
            CancellationToken cancellationToken)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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
