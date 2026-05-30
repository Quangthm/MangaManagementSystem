using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesContributorDto(
        long SeriesContributorId,
        long SeriesId,
        int UserId,
        DateTime StartDate,
        DateTime? EndDate,
        string? Notes
    );

    public record CreateSeriesContributorDto(
        [Required] long SeriesId,
        [Required] int UserId,
        [Required] DateTime StartDate,
        DateTime? EndDate,
        string? Notes
    );

    public record UpdateSeriesContributorDto(
        [Required] long SeriesContributorId,
        [Required] long SeriesId,
        [Required] int UserId,
        [Required] DateTime StartDate,
        DateTime? EndDate,
        string? Notes
    );
}
