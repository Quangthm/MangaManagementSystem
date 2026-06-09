using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesContributorDto(
        Guid SeriesContributorId,
        Guid SeriesId,
        Guid UserId,
        DateTime StartDate,
        DateTime? EndDate,
        string? Notes
    );

    public record CreateSeriesContributorDto(
        [Required] Guid SeriesId,
        [Required] Guid UserId,
        [Required] DateTime StartDate,
        DateTime? EndDate,
        string? Notes
    );

    public record UpdateSeriesContributorDto(
        [Required] Guid SeriesContributorId,
        [Required] Guid SeriesId,
        [Required] Guid UserId,
        [Required] DateTime StartDate,
        DateTime? EndDate,
        string? Notes
    );
}
