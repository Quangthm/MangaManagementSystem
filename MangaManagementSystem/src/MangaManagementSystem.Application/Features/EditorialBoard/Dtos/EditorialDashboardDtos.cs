namespace MangaManagementSystem.Application.Features.EditorialBoard.Dtos;

public sealed record EditorialDashboardDto(
    int ProposalReviewCount,
    int OpenPollCount,
    int AwaitingDecisionCount,
    IReadOnlyList<EditorialProposalReviewRowDto> RecentProposals,
    IReadOnlyList<EditorialOpenPollRowDto> OpenPolls,
    IReadOnlyList<EditorialDecisionQueueRowDto> Decisions);

public sealed record EditorialProposalReviewRowDto(
    long ProposalId,
    long SeriesId,
    string Code,
    string Title,
    string Author,
    string Genre,
    string Status);

public sealed record EditorialOpenPollRowDto(
    long PollId,
    long SeriesId,
    string Code,
    string Name,
    int ApproveVotes,
    int RejectVotes,
    int AbstainVotes,
    int TotalVotes,
    string Status);

public sealed record EditorialDecisionQueueRowDto(
    long PollId,
    long SeriesId,
    string Code,
    string Title,
    int ApproveVotes,
    int RejectVotes,
    int AbstainVotes,
    int TotalVotes,
    string ComputedResultCode);