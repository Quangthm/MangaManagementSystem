using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Input for the Mangaka "create a new series draft" use case.
    /// A draft creates only a <c>manga.Series</c> row with status <c>PROPOSAL_DRAFT</c>
    /// plus an optional <c>SERIES_COVER</c> file. It never creates a SeriesProposal and
    /// never carries a proposal document (that belongs to the later Submit Proposal workflow).
    /// </summary>
    public record CreateSeriesDraftDto(
        [Required][MaxLength(200)] string Title,
        [Required] string Synopsis,
        [Required][MaxLength(100)] string Genre,
        [MaxLength(10)] string? ContentLanguageCode,
        [MaxLength(220)] string? Slug,
        [MaxLength(50)] string? PublicationFrequencyCode,
        Guid? SourceSeriesId,
        byte[]? CoverFileBytes,
        string? CoverFileName,
        string? CoverContentType
    );
}
