using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterPageTask
    {
        public Guid ChapterPageTaskId { get; set; }
        public Guid AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }
        public string TypeCode { get; set; } = null!;
        public string StatusCode { get; set; } = "ASSIGNED";
        public string TaskTitle { get; set; } = null!;
        public string TaskDescription { get; set; } = null!;
        public byte PriorityLevel { get; set; } = 3;
        public DateTime DueAtUtc { get; set; }
        public decimal? CompensationAmount { get; set; }
        public Guid? CompletedPageVersionId { get; set; }
        public ChapterPageVersion? CompletedPageVersion { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        // Page context is derived through linked PageRegions
        // (ChapterPageTask -> PageRegions -> ChapterPageVersion -> ChapterPage -> Chapter).
        // Mapped to the manga.ChapterPageTaskRegion junction table via EF skip navigation.
        public ICollection<PageRegion> PageRegions { get; set; } = new List<PageRegion>();
    }
}

