using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record PageRegionDto(
        long PageRegionId,
        long ChapterPageVersionId,
        string TypeCode,
        string? RegionLabel,
        decimal X,
        decimal Y,
        decimal Width,
        decimal Height,
        decimal? ConfidenceScore,
        string SourceType,
        string? OriginalText,
        int? CreatedByUserId,
        int? UpdatedByUserId
    );

    public record CreatePageRegionDto(
        [Required] long ChapterPageVersionId,
        [Required][MaxLength(80)] string TypeCode,
        string? RegionLabel,
        [Required] decimal X,
        [Required] decimal Y,
        [Required] decimal Width,
        [Required] decimal Height,
        decimal? ConfidenceScore,
        [Required][MaxLength(20)] string SourceType,
        string? OriginalText
    );

    public record UpdatePageRegionDto(
        [Required] long PageRegionId,
        [Required] long ChapterPageVersionId,
        [Required][MaxLength(80)] string TypeCode,
        string? RegionLabel,
        [Required] decimal X,
        [Required] decimal Y,
        [Required] decimal Width,
        [Required] decimal Height,
        decimal? ConfidenceScore,
        [Required][MaxLength(20)] string SourceType,
        string? OriginalText
    );
}
