using System;

namespace MangaManagementSystem.Domain.Entities
{
    /// <summary>
    /// Read-only projection of manga.vw_SeriesBoardPollVoteSummary.
    /// </summary>
    public class SeriesBoardPollVoteSummary
    {
        public Guid SeriesBoardPollId { get; set; }
        public Guid SeriesId { get; set; }
        public string SeriesTitle { get; set; } = null!;
        public string PollTypeCode { get; set; } = null!;
        public string PollStatusCode { get; set; } = null!;
        public string PollReason { get; set; } = null!;
        public Guid CreatedByUserId { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        public int ApproveCount { get; set; }
        public int RejectCount { get; set; }
        public int AbstainCount { get; set; }
        public int TotalVoteCount { get; set; }
        public string? ComputedResultCode { get; set; }
        public bool IsApplicable { get; set; }
    }
}
