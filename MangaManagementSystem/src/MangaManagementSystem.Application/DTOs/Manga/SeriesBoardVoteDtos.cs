using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesBoardVoteDto(
        Guid SeriesBoardVoteId,
        Guid SeriesBoardPollId,
        Guid UserId,
        string ChoiceCode,
        string? VoteReason,
        DateTime VotedAtUtc
    );

    public record CreateSeriesBoardVoteDto(
        [Required] Guid SeriesBoardPollId,
        [Required] Guid UserId,
        [Required][MaxLength(50)] string ChoiceCode,
        string? VoteReason
    );
}
