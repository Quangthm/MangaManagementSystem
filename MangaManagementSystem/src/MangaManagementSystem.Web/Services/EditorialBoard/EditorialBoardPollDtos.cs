using System;

namespace MangaManagementSystem.Web.Services.EditorialBoard;

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
    string? CurrentUserVoteReason,
    string Author,
    string Genre,
    string TagsDisplay,
    string Synopsis,
    string? CoverImageUrl);

public sealed record OpenPollRequest(
    string PollTypeCode,
    string PollReason,
    string? PublicationFrequencyCode,
    DateTime? EndsAtUtc);

public sealed record OpenPollResult(
    Guid PollId,
    Guid SeriesId,
    Guid ProposalId,
    string PollStatusCode);

public sealed record CastVoteRequest(
    string ChoiceCode,
    string? VoteReason);

public sealed record CastVoteResult(
    Guid PollId,
    Guid VoteId,
    Guid UserId,
    string ChoiceCode,
    string? VoteReason,
    DateTime VotedAtUtc);

public sealed record FinalizePollResult(
    Guid PollId,
    Guid SeriesId,
    string PollStatusCode,
    string SeriesStatusCode,
    string? PublicationFrequencyCode,
    DateTime EndedAtUtc);

public sealed record CancellableBoardSeriesDto(
    Guid SeriesId,
    string Code,
    string Title,
    string Author,
    string Genre,
    string TagsDisplay,
    string Synopsis,
    string? PublicationFrequencyCode,
    string StatusCode,
    string? CoverImageUrl,
    bool HasOpenCancelSerializationPoll);

public sealed record OpenCancelSerializationPollRequest(
    string PollReason,
    DateTime? EndsAtUtc);

public sealed record UpdateBoardPollDeadlineRequest(
    DateTime? EndsAtUtc);

public sealed record UpdateBoardPollDeadlineResult(
    Guid PollId,
    Guid SeriesId,
    string PollStatusCode,
    DateTime? EndsAtUtc);

public sealed record UpdateSeriesPublicationFrequencyRequest(
    string PublicationFrequencyCode,
    string Reason);

public sealed record UpdateSeriesPublicationFrequencyResult(
    Guid SeriesId,
    string Title,
    string? OldPublicationFrequencyCode,
    string NewPublicationFrequencyCode,
    DateTime UpdatedAtUtc);