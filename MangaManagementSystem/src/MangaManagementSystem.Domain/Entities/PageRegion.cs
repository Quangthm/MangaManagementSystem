using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class PageRegion : BaseEntity
    {
        public long PageRegionId { get; set; }
        public long ChapterPageVersionId { get; set; }
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
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }
        public User? UpdatedByUser { get; set; }
        public User? DeletedByUser { get; set; }
        public int? DeletedByUserId { get; set; }
    }
}
