using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class SeriesProposal
    {
        public long SeriesProposalId { get; set; }
        public long SeriesId { get; set; }
        public Series? Series { get; set; }
        public short ProposalVersionNo { get; set; }
        public string ProposalTitle { get; set; } = null!;
        public string SynopsisSnapshot { get; set; } = null!;
        public string GenreSnapshot { get; set; } = null!;
        public long ProposalFileId { get; set; }
        public FileResource? ProposalFile { get; set; }
        public string StatusCode { get; set; } = "UNDER_EDITORIAL_REVIEW";
        public int SubmittedByUserId { get; set; }
        public User? SubmittedByUser { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public DateTime? WithdrawnAtUtc { get; set; }
        public int? ReviewedByUserId { get; set; }
        public User? ReviewedByUser { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string? Comments { get; set; }
        public long? MarkupFileId { get; set; }
        public FileResource? MarkupFile { get; set; }
    }
}
