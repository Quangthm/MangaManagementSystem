using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesBoardPollDto(
        long SeriesBoardPollId,
        long SeriesId,
        string PollTypeCode,
        string PollReason,
        string PollStatusCode,
        int CreatedByUserId,
        DateTime StartedAtUtc,
        DateTime? EndsAtUtc
    );

    public record CreateSeriesBoardPollDto(
        [Required] long SeriesId,
        [Required][MaxLength(50)] string PollTypeCode,
        [Required] string PollReason,
        [Required] int CreatedByUserId,
        DateTime? EndsAtUtc
    );

    public record UpdateSeriesBoardPollDto(
        [Required] long SeriesBoardPollId,
        [Required][MaxLength(50)] string PollStatusCode
    );
}
