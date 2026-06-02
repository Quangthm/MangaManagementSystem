using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesBoardVoteDto(
        long SeriesBoardVoteId,
        long SeriesBoardPollId,
        int UserId,
        string ChoiceCode,
        string? VoteReason,
        DateTime VotedAtUtc
    );

    public record CreateSeriesBoardVoteDto(
        [Required] long SeriesBoardPollId,
        [Required] int UserId,
        [Required][MaxLength(50)] string ChoiceCode,
        string? VoteReason
    );
}
