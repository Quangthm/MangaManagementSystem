using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterPageTask : BaseEntity
    {
        public long ChapterPageTaskId { get; set; }
        public int AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }
        public string TypeCode { get; set; } = null!;
        public string StatusCode { get; set; } = "ASSIGNED";
        public int PriorityLevel { get; set; } = 3;
        public DateTime? DueAtUtc { get; set; }
        public long? CompletedPageVersionId { get; set; }
        public ChapterPageVersion? CompletedPageVersion { get; set; }
    }
}
