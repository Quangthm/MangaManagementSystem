using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterPageAnnotation : BaseEntity
    {
        public long ChapterPageAnnotationId { get; set; }
        public long PageRegionId { get; set; }
        public PageRegion? PageRegion { get; set; }
        public string IssueTypeCode { get; set; } = null!;
        public int AnnotatedByUserId { get; set; }
        public User? AnnotatedByUser { get; set; }
        public string? AnnotationText { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public int? ResolvedByUserId { get; set; }
        public User? ResolvedByUser { get; set; }
    }
}
