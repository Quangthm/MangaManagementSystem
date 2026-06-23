using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesDto(
        Guid SeriesId,
        string Title,
        string Slug,
        string Synopsis,
        IReadOnlyList<GenreDto> Genres,
        IReadOnlyList<TagDto> Tags,
        Guid? CoverFileId,
        string StatusCode,
        string ContentLanguageCode,
        Guid? SourceSeriesId,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        Guid? UpdatedByUserId,
        string? PublicationFrequencyCode,
        /// <summary>
        /// Cloudinary secure URL for the series cover image, if a cover FileResource exists
        /// and has not been soft-deleted. Null when no cover has been uploaded.
        /// Display-only — do not use for upload or update workflows.
        /// </summary>
        string? CoverUrl = null
    );

    public record CreateSeriesDto(
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
        [Required][MaxLength(200)] string Title,
        [Required][MaxLength(220)] string Slug,
        [Required] string Synopsis,
        [Required][MaxLength(100)] string Genre,
        Guid? CoverFileId,
        [MaxLength(50)] string StatusCode,
        [MaxLength(10)] string ContentLanguageCode,
        Guid? SourceSeriesId,
        [MaxLength(20)] string? PublicationFrequencyCode,
        Guid? UpdatedByUserId
    );
}
