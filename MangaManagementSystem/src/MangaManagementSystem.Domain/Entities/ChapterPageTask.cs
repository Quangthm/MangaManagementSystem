using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterPageTask
    {
        public long ChapterPageTaskId { get; set; }
        public long ChapterPageId { get; set; }
        public ChapterPage? ChapterPage { get; set; }
        public int AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }
        public string TypeCode { get; set; } = null!;
        public string StatusCode { get; set; } = "ASSIGNED";
        public string TaskTitle { get; set; } = null!;
        public string TaskDescription { get; set; } = null!;
        public byte PriorityLevel { get; set; } = 3;
        public DateTime DueAtUtc { get; set; }
        public decimal? CompensationAmount { get; set; }
        public long? CompletedPageVersionId { get; set; }
        public ChapterPageVersion? CompletedPageVersion { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
