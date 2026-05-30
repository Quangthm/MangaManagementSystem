using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class Chapter : BaseEntity
    {
        public long ChapterId { get; set; }
        public long SeriesId { get; set; }
        public Series? Series { get; set; }
        public string ChapterNumberLabel { get; set; } = null!;
        public string? ChapterTitle { get; set; }
        public string StatusCode { get; set; } = "DRAFT";
        public DateTime? PlannedReleaseDate { get; set; }
        public DateTime? ReleasedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
