using MangaManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
public class PageRegion : BaseEntity
{
    public Guid PageRegionId { get; set; }
    public Guid ChapterPageVersionId { get; set; }
    public ChapterPageVersion? ChapterPageVersion { get; set; }
    public string TypeCode { get; set; } = "OTHER";
    public string? RegionLabel { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string SourceType { get; set; } = "MANUAL";
    public string? OriginalText { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    // Skip-navigation collections mapped through junction tables
    public ICollection<ChapterPageTask> Tasks { get; set; } = new List<ChapterPageTask>();
    public ICollection<ChapterPageAnnotation> Annotations { get; set; } = new List<ChapterPageAnnotation>();
    }
}
