using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record PageRegionDto(
        Guid PageRegionId,
        Guid ChapterPageVersionId,
        string TypeCode,
        string? RegionLabel,
        decimal X,
        decimal Y,
        decimal Width,
        decimal Height,
        decimal? ConfidenceScore,
        string SourceType,
        string? OriginalText,
        Guid? CreatedByUserId,
        Guid? UpdatedByUserId
    );

    public record CreatePageRegionDto(
        [Required] Guid ChapterPageVersionId,
        [Required][MaxLength(80)] string TypeCode,
        string? RegionLabel,
        [Required] decimal X,
        [Required] decimal Y,
        [Required] decimal Width,
        [Required] decimal Height,
        decimal? ConfidenceScore,
        [Required][MaxLength(20)] string SourceType,
        string? OriginalText,
        Guid? PageRegionId = null,
        Guid? CreatedByUserId = null
    );

    /// <summary>Web → API body carrying a set of ChapterPageVersion ids (region reads/counts).</summary>
    public sealed record VersionIdsRequest(IReadOnlyList<Guid> VersionIds);

    /// <summary>Web → API body to bulk-replace all regions of one version.</summary>
    public sealed record BulkReplaceRegionsRequest(IReadOnlyList<CreatePageRegionDto> Regions);

    public record UpdatePageRegionDto(
        [Required] Guid PageRegionId,
        [Required] Guid ChapterPageVersionId,
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
