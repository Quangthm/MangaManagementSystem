using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesBoardPollDto(
        Guid SeriesBoardPollId,
        Guid SeriesId,
        string PollTypeCode,
        string PollReason,
        string PollStatusCode,
        Guid CreatedByUserId,
        DateTime StartedAtUtc,
        DateTime? EndsAtUtc
    );

    public record CreateSeriesBoardPollDto(
        [Required] Guid SeriesId,
        [Required][MaxLength(50)] string PollTypeCode,
        [Required] string PollReason,
        [Required] Guid CreatedByUserId,
        DateTime? EndsAtUtc
    );

    public record UpdateSeriesBoardPollDto(
        [Required] Guid SeriesBoardPollId,
        [Required][MaxLength(50)] string PollStatusCode
    );
}
