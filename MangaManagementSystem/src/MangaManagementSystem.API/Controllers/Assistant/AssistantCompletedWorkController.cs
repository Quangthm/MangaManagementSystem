using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Assistant.CompletedWork.Queries.GetAssistantCompletedWork;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Assistant;

[ApiController]
[Authorize]
[Route("api/assistant/completed-work")]
public sealed class AssistantCompletedWorkController : BaseApiController
{

    private readonly IMediator _mediator;
    private readonly ILogger<AssistantCompletedWorkController> _logger;

    public AssistantCompletedWorkController(
        IMediator mediator,
        ILogger<AssistantCompletedWorkController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> GetCompletedWorkAsync(CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId();

        try
        {
            AssistantCompletedWorkSummaryDto result =
                await _mediator.Send(
                    new GetAssistantCompletedWorkQuery(actorUserId), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error loading completed work summary for user {ActorUserId}.",
                actorUserId);
            return Problem(
                detail: "We could not load your completed work summary right now. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

}

