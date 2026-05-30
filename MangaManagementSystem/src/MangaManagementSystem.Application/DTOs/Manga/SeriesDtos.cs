using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesDto(
        long SeriesId,
        string SeriesCode,
        string? Title,
        string? Slug,
        string? Synopsis,
        string? Genre,
        long? CoverFileId,
        string StatusCode,
        string ContentLanguageCode,
        long? SourceSeriesId,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        int? UpdatedByUserId,
        string? PublicationFrequencyCode
    );

    public record CreateSeriesDto(
        [Required][MaxLength(50)] string SeriesCode,
        [MaxLength(200)] string? Title,
        [MaxLength(220)] string? Slug,
        string? Synopsis,
        [MaxLength(100)] string? Genre,
        long? CoverFileId,
        [MaxLength(50)] string StatusCode,
        [MaxLength(10)] string ContentLanguageCode,
        long? SourceSeriesId,
        [MaxLength(20)] string? PublicationFrequencyCode
    );

    public record UpdateSeriesDto(
        [Required] long SeriesId,
        [Required][MaxLength(50)] string SeriesCode,
        [MaxLength(200)] string? Title,
        [MaxLength(220)] string? Slug,
        string? Synopsis,
        [MaxLength(100)] string? Genre,
        long? CoverFileId,
        [MaxLength(50)] string StatusCode,
        [MaxLength(10)] string ContentLanguageCode,
        long? SourceSeriesId,
        [MaxLength(20)] string? PublicationFrequencyCode
    );
}
