using MangaManagementSystem.API.Contracts.BoardPolls;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers;

[ApiController]
[Route("api/board-polls")]
public sealed class BoardPollsController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Editorial Board Member,Editorial Board Chief")]
    public IActionResult GetBoardPolls()
    {
        // TODO: Call Application service later.
        return Ok(new
        {
            message = "Board polls list endpoint. Board Member and Board Chief can view."
        });
    }

    [HttpGet("{pollId:guid}/results")]
    [Authorize(Roles = "Editorial Board Member,Editorial Board Chief")]
    public IActionResult GetPollResults(Guid pollId)
    {
        // TODO: Read from manga.vw_SeriesBoardPollVoteSummary later.
        return Ok(new
        {
            pollId,
            message = "Poll results endpoint. Board Member and Board Chief can view."
        });
    }

    [HttpPost]
    [Authorize(Roles = "Editorial Board Chief")]
    public IActionResult OpenPoll([FromBody] OpenBoardPollRequest request)
    {
        // TODO: Call Application service / stored procedure later.
        return Ok(new
        {
            message = "Open poll endpoint. Only Editorial Board Chief can call this.",
            request.SeriesId,
            request.PollTypeCode,
            request.EndsAtUtc
        });
    }

    [HttpPatch("{pollId:guid}/deadline")]
    [Authorize(Roles = "Editorial Board Chief")]
    public IActionResult UpdateDeadline(
        Guid pollId,
        [FromBody] UpdateBoardPollDeadlineRequest request)
    {
        // TODO: Update manga.SeriesBoardPoll.ends_at_utc later.
        return Ok(new
        {
            message = "Update deadline endpoint. Only Editorial Board Chief can call this.",
            pollId,
            request.EndsAtUtc
        });
    }

    [HttpPost("{pollId:guid}/final-approval")]
    [Authorize(Roles = "Editorial Board Chief")]
    public IActionResult FinalApproval(Guid pollId)
    {
        // TODO: Call final approval workflow later.
        return Ok(new
        {
            message = "Final approval endpoint. Only Editorial Board Chief can call this.",
            pollId
        });
    }

    [HttpPost("{pollId:guid}/return-for-review")]
    [Authorize(Roles = "Editorial Board Chief")]
    public IActionResult ReturnForReview(Guid pollId)
    {
        // TODO: Call return-for-review workflow later.
        return Ok(new
        {
            message = "Return for review endpoint. Only Editorial Board Chief can call this.",
            pollId
        });
    }

    [HttpPost("{pollId:guid}/escalate")]
    [Authorize(Roles = "Editorial Board Chief")]
    public IActionResult EscalateDecision(Guid pollId)
    {
        // TODO: Call escalation workflow later.
        return Ok(new
        {
            message = "Escalate decision endpoint. Only Editorial Board Chief can call this.",
            pollId
        });
    }
}