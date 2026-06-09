using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterReaderVoteSnapshot
    {
        public Guid ChapterReaderVoteSnapshotId { get; set; }
        public Guid ChapterId { get; set; }
        public Chapter? Chapter { get; set; }
        public int ReaderVoteCount { get; set; }
        public decimal? AverageRating { get; set; }
        public int? PositiveFeedbackCount { get; set; }
        public int? NegativeFeedbackCount { get; set; }
        public string? DataSourceNote { get; set; }
        public Guid EnteredByUserId { get; set; }
        public User? EnteredByUser { get; set; }
        public DateTime VotedAtUtc { get; set; }
    }
}
