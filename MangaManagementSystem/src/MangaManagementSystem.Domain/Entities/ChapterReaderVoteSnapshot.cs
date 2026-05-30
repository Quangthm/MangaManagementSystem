using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterReaderVoteSnapshot : BaseEntity
    {
        public long ChapterReaderVoteSnapshotId { get; set; }
        public long ChapterId { get; set; }
        public Chapter? Chapter { get; set; }
        public int ReaderVoteCount { get; set; }
        public decimal AverageRating { get; set; }
        public int? EnteredByUserId { get; set; }
        public User? EnteredByUser { get; set; }
    }
}
