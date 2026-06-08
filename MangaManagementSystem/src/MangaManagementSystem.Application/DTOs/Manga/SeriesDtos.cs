using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesDto(
        Guid SeriesId,
        string SeriesCode,
        string Title,
        string Slug,
        string Synopsis,
        string Genre,
        Guid? CoverFileId,
        string StatusCode,
        string ContentLanguageCode,
        Guid? SourceSeriesId,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        Guid? UpdatedByUserId,
        string? PublicationFrequencyCode
    );

    public record CreateSeriesDto(
        [Required][MaxLength(50)] string SeriesCode,
        [Required][MaxLength(200)] string Title,
        [Required][MaxLength(220)] string Slug,
        [Required] string Synopsis,
        [Required][MaxLength(100)] string Genre,
        Guid? CoverFileId,
        [MaxLength(50)] string StatusCode,
        [MaxLength(10)] string ContentLanguageCode,
        Guid? SourceSeriesId,
        [MaxLength(20)] string? PublicationFrequencyCode
    );

    public record UpdateSeriesDto(
        [Required] Guid SeriesId,
        [Required][MaxLength(50)] string SeriesCode,
        [Required][MaxLength(200)] string Title,
        [Required][MaxLength(220)] string Slug,
        [Required] string Synopsis,
        [Required][MaxLength(100)] string Genre,
        Guid? CoverFileId,
        [MaxLength(50)] string StatusCode,
        [MaxLength(10)] string ContentLanguageCode,
        Guid? SourceSeriesId,
        [MaxLength(20)] string? PublicationFrequencyCode
    );
}
