namespace MangaManagementSystem.Application.Features.EditorialBoard.Dtos;

public sealed record EditorialBoardPollDto(
    Guid PollId,
    Guid SeriesId,
    string Code,
    string SeriesTitle,
    string PollName,
    string PollTypeCode,
    string PollStatusCode,
    string PollReason,
    string? PublicationFrequencyCode,
    DateTime StartedAtUtc,
    DateTime? EndsAtUtc,
    int ApproveVotes,
    int RejectVotes,
    int AbstainVotes,
    int TotalVotes,
    string ComputedResultCode,
    string? CurrentUserChoiceCode,
    string? CurrentUserVoteReason);

public sealed record OpenSeriesBoardPollRequestDto(
    Guid ProposalId,
    string PollTypeCode,
    string PollReason,
    string? PublicationFrequencyCode,
    DateTime? EndsAtUtc);

public sealed record OpenSeriesBoardPollResultDto(
    Guid PollId,
    Guid SeriesId,
    Guid ProposalId,
    string PollStatusCode);

public sealed record CastSeriesBoardVoteRequestDto(
    Guid PollId,
    string ChoiceCode,
    string? VoteReason);

public sealed record CastSeriesBoardVoteResultDto(
    Guid PollId,
    Guid VoteId,
    Guid UserId,
    string ChoiceCode,
    string? VoteReason,
    DateTime VotedAtUtc);

public sealed record FinalizeBoardPollResultDto(
    Guid PollId,
    Guid SeriesId,
    string PollStatusCode,
    string SeriesStatusCode,
    string? PublicationFrequencyCode,
    DateTime EndedAtUtc);