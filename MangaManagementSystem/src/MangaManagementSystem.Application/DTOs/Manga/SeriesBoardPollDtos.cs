using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesBoardPollDto(
        long SeriesBoardPollId,
        long SeriesId,
        string PollTypeCode,
        string PollStatusCode,
        DateTime CreatedAtUtc
    );

    public record CreateSeriesBoardPollDto(
        [Required] long SeriesId,
        [Required][MaxLength(50)] string PollTypeCode
    );

    public record UpdateSeriesBoardPollDto(
        [Required] long SeriesBoardPollId,
        [Required][MaxLength(20)] string PollStatusCode
    );
}
