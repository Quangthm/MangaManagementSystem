using System.Security.Claims;
using MangaManagementSystem.Application.Features.EditorialBoard.Commands;
using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers.Board;

[ApiController]
[Route("api/editorial-board")]
[Authorize]
public sealed class EditorialBoardController : BaseApiController
{
    private readonly IMediator _mediator;

    public EditorialBoardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetEditorialDashboardQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("polls/open")]
    public async Task<IActionResult> GetOpenPolls(CancellationToken cancellationToken)
    {
        var currentUserId = ResolveActorUserId();

        var result = await _mediator.Send(
            new GetOpenBoardPollsQuery(currentUserId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("polls/history")]
    public async Task<IActionResult> GetPollHistory(CancellationToken cancellationToken)
    {
        var currentUserId = ResolveActorUserId();

        var result = await _mediator.Send(
            new GetBoardPollHistoryQuery(currentUserId),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("proposals/{proposalId:guid}/polls")]
    [Authorize(Roles = "Editorial Board Chief")]
    public async Task<IActionResult> OpenPoll(
        Guid proposalId,
        [FromBody] OpenPollApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var chiefUserId = ResolveActorUserId();

            var commandRequest = new OpenSeriesBoardPollRequestDto(
                ProposalId: proposalId,
                PollTypeCode: request.PollTypeCode,
                PollReason: request.PollReason,
                PublicationFrequencyCode: request.PublicationFrequencyCode,
                EndsAtUtc: request.EndsAtUtc);

            var result = await _mediator.Send(
                new OpenSeriesBoardPollCommand(
                    ChiefUserId: chiefUserId,
                    Request: commandRequest),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("polls/{pollId:guid}/votes")]
    [Authorize(Roles = "Editorial Board Chief,Editorial Board Member")]
    public async Task<IActionResult> CastVote(
        Guid pollId,
        [FromBody] CastVoteApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var voterUserId = ResolveActorUserId();

            var commandRequest = new CastSeriesBoardVoteRequestDto(
                PollId: pollId,
                ChoiceCode: request.ChoiceCode,
                VoteReason: request.VoteReason);

            var result = await _mediator.Send(
                new CastSeriesBoardVoteCommand(
                    VoterUserId: voterUserId,
                    Request: commandRequest),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("polls/{pollId:guid}/final-approval")]
    [Authorize(Roles = "Editorial Board Chief")]
    public async Task<IActionResult> FinalApproval(
        Guid pollId,
        CancellationToken cancellationToken)
    {
        try
        {
            var chiefUserId = ResolveActorUserId();

            var result = await _mediator.Send(
                new FinalizeBoardPollApprovalCommand(
                    PollId: pollId,
                    ChiefUserId: chiefUserId),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("polls/{pollId:guid}/cancel")]
    [Authorize(Roles = "Editorial Board Chief")]
    public async Task<IActionResult> CancelPoll(
        Guid pollId,
        CancellationToken cancellationToken)
    {
        try
        {
            var chiefUserId = ResolveActorUserId();

            var result = await _mediator.Send(
                new CancelBoardPollCommand(
                    PollId: pollId,
                    ChiefUserId: chiefUserId),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public sealed record OpenPollApiRequest(
        string PollTypeCode,
        string PollReason,
        string? PublicationFrequencyCode,
        DateTime? EndsAtUtc);

    public sealed record CastVoteApiRequest(
        string ChoiceCode,
        string? VoteReason);
}