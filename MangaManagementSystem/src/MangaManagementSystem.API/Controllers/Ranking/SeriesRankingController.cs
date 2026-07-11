using System.Security.Claims;
using MangaManagementSystem.API.Contracts.Ranking;
using MangaManagementSystem.Application.Features.Ranking.Commands;
using MangaManagementSystem.Application.Features.Ranking.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers.Ranking;

[ApiController]
[Route("api/ranking")]
[Authorize(Roles = "Mangaka,Assistant,Tantou Editor,Editorial Board Member,Editorial Board Chief,EditorialBoardMember,EditorialBoardChief,Board Member,Board Chief,Admin")]
public sealed class SeriesRankingController : ControllerBase
{
    private const string BoardRankingInputRoles =
        "Editorial Board Member,Editorial Board Chief,EditorialBoardMember,EditorialBoardChief,Board Member,Board Chief";

    private readonly IMediator _mediator;

    public SeriesRankingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("periods/weekly")]
    public async Task<IActionResult> GetWeeklyPeriods(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetWeeklyPublicationPeriodsQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("periods/{publicationPeriodId:guid}/vote-inputs")]
    public async Task<IActionResult> GetVoteInputs(
        Guid publicationPeriodId,
        [FromQuery] string? searchText,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSeriesVoteInputsQuery(publicationPeriodId, searchText, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("periods/{publicationPeriodId:guid}/results")]
    public async Task<IActionResult> GetRanking(
        Guid publicationPeriodId,
        [FromQuery] string? searchText,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSeriesRankingQuery(publicationPeriodId, searchText, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("periods/{publicationPeriodId:guid}/series-suggestions")]
    [Authorize(Roles = BoardRankingInputRoles)]
    public async Task<IActionResult> SearchSeries(
        Guid publicationPeriodId,
        [FromQuery] string? searchText,
        [FromQuery] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new SearchRankableSeriesQuery(publicationPeriodId, searchText, maxResults),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("vote-inputs")]
    [Authorize(Roles = BoardRankingInputRoles)]
    public async Task<IActionResult> CreateVoteInput(
        [FromBody] CreateSeriesVoteInputRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = GetCurrentUserId();

            var result = await _mediator.Send(
                new CreateSeriesVoteInputCommand(
                    actorUserId,
                    request.PublicationPeriodId,
                    request.SeriesId,
                    request.RatingCount,
                    request.AverageRating,
                    request.ReadingCount,
                    request.DataSourceNote),
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    code = "RANKING_FORBIDDEN",
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    code = "RANKING_VALIDATION_FAILED",
                    message = ex.Message
                });
        }
    }

    [HttpPut("vote-inputs/{seriesVoteInputId:guid}")]
    [Authorize(Roles = BoardRankingInputRoles)]
    public async Task<IActionResult> UpdateVoteInput(
        Guid seriesVoteInputId,
        [FromBody] UpdateSeriesVoteInputRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = GetCurrentUserId();

            var result = await _mediator.Send(
                new UpdateSeriesVoteInputCommand(
                    actorUserId,
                    seriesVoteInputId,
                    request.RatingCount,
                    request.AverageRating,
                    request.ReadingCount,
                    request.DataSourceNote),
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    code = "RANKING_FORBIDDEN",
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    code = "RANKING_VALIDATION_FAILED",
                    message = ex.Message
                });
        }
    }

    private Guid GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id")
            ?? User.FindFirstValue("UserId");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Cannot resolve current user id from token.");
        }

        return userId;
    }
}