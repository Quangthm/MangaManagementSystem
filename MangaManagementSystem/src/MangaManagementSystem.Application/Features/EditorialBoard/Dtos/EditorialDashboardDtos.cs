using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Dtos;

public sealed record EditorialDashboardDto(
    int ProposalReviewCount,
    int OpenPollCount,
    int AwaitingDecisionCount,
    IReadOnlyList<EditorialProposalReviewRowDto> RecentProposals,
    IReadOnlyList<EditorialOpenPollRowDto> OpenPolls,
    IReadOnlyList<EditorialDecisionQueueRowDto> Decisions);

public sealed record EditorialProposalReviewRowDto(
    Guid ProposalId,
    Guid SeriesId,
    string Code,
    string Title,
    string Author,
    string Genre,
    string Status);

public sealed record EditorialOpenPollRowDto(
    Guid PollId,
    Guid SeriesId,
    string Code,
    string Name,
    int ApproveVotes,
    int RejectVotes,
    int AbstainVotes,
    int TotalVotes,
    string Status);

public sealed record EditorialDecisionQueueRowDto(
    Guid PollId,
    Guid SeriesId,
    string Code,
    string Title,
    int ApproveVotes,
    int RejectVotes,
    int AbstainVotes,
    int TotalVotes,
    string ComputedResultCode);