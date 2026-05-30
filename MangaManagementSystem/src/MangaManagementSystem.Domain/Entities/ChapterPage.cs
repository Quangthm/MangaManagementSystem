using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterPage : BaseEntity
    {
        public long ChapterPageId { get; set; }
        public long ChapterId { get; set; }
        public Chapter? Chapter { get; set; }
        public int PageNo { get; set; }
        public string? PageNotes { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public int? DeletedByUserId { get; set; }
        public User? DeletedByUser { get; set; }
    }
}
