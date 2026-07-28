using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Assistant.CompletedWork.Queries.GetAssistantCompletedWork;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Assistant
{
    [ApiController]
    [Authorize(Roles = AssistantRoleName)]
    [Route("api/assistant/completed-work")]
    public class AssistantCompletedWorkController : ControllerBase
    {
        private const string AssistantRoleName = "Assistant";

        private readonly IMediator _mediator;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly ILogger<AssistantCompletedWorkController> _logger;

        public AssistantCompletedWorkController(
            IMediator mediator,
            IAuthenticatedActorResolver actorResolver,
            ILogger<AssistantCompletedWorkController> logger)
        {
            _mediator =
                mediator
                ?? throw new ArgumentNullException(
                    nameof(mediator));

            _actorResolver =
                actorResolver
                ?? throw new ArgumentNullException(
                    nameof(actorResolver));

            _logger =
                logger
                ?? throw new ArgumentNullException(
                    nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult>
            GetCompletedWorkAsync(
                CancellationToken cancellationToken)
        {
            var (actorUserId, actorFailure) =
                await ResolveActorAsync();

            if (actorFailure is not null)
            {
                return actorFailure;
            }

            try
            {
                AssistantCompletedWorkSummaryDto result =
                    await _mediator.Send(
                        new GetAssistantCompletedWorkQuery(
                            actorUserId),
                        cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error loading completed work summary for user {ActorUserId}.",
                    actorUserId);

                return Problem(
                    detail:
                        "We could not load your completed work summary right now. Please try again later.",
                    statusCode:
                        StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<
            (Guid ActorUserId, IActionResult? Failure)>
            ResolveActorAsync()
        {
            var result =
                await _actorResolver.ResolveAsync(
                    User,
                    AssistantRoleName);

            if (result.Succeeded)
            {
                return (
                    result.ActorUserId,
                    null);
            }

            var response =
                new ApiErrorResponse(
                    result.FailureKind
                        == AuthenticatedActorFailureKind.UserNotFound
                        ? "Authenticated Assistant account was not found."
                        : result.FailureKind
                            == AuthenticatedActorFailureKind.InvalidIdentity
                            ? "Authenticated Assistant information is invalid."
                            : "The current account is not an active Assistant.");

            return result.FailureKind
                is AuthenticatedActorFailureKind.InvalidIdentity
                or AuthenticatedActorFailureKind.UserNotFound
                    ? (
                        Guid.Empty,
                        Unauthorized(response))
                    : (
                        Guid.Empty,
                        StatusCode(
                            StatusCodes.Status403Forbidden,
                            response));
        }
    }
}
