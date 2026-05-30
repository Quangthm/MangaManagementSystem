using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesBoardVoteDto(
        long SeriesBoardVoteId,
        long SeriesBoardPollId,
        int UserId,
        string ChoiceCode,
        string? Reason,
        DateTime CreatedAtUtc
    );

    public record CreateSeriesBoardVoteDto(
        [Required] long SeriesBoardPollId,
        [Required] int UserId,
        [Required][MaxLength(20)] string ChoiceCode,
        string? Reason
    );
}
