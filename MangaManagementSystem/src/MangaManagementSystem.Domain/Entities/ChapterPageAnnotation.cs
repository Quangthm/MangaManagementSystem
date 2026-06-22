using MangaManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterPageAnnotation : BaseEntity
    {
        public Guid ChapterPageAnnotationId { get; set; }
        public string IssueTypeCode { get; set; } = null!;
        public Guid AnnotatedByUserId { get; set; }
        public User? AnnotatedByUser { get; set; }
        public string? AnnotationText { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public Guid? ResolvedByUserId { get; set; }
        public User? ResolvedByUser { get; set; }

        // Annotation region context is represented through linked PageRegions.
        // Mapped to the manga.ChapterPageAnnotationRegion junction table via EF skip navigation.
        public ICollection<PageRegion> PageRegions { get; set; } = new List<PageRegion>();
    }
}
