using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class SeriesProposal : BaseEntity
    {
        public long SeriesProposalId { get; set; }
        public long SeriesId { get; set; }
        public Series? Series { get; set; }
        public short ProposalVersionNo { get; set; }
        public string? ProposalTitle { get; set; }
        public string? SynopsisSnapshot { get; set; }
        public string? GenreSnapshot { get; set; }
        public long? ProposalFileId { get; set; }
        public FileResource? ProposalFile { get; set; }
        public string StatusCode { get; set; } = "UNDER_EDITORIAL_REVIEW";
        public int? SubmittedByUserId { get; set; }
        public User? SubmittedByUser { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
        public DateTime? WithdrawnAtUtc { get; set; }
        public int? ReviewedByUserId { get; set; }
        public User? ReviewedByUser { get; set; }
        public long? MarkupFileId { get; set; }
        public FileResource? MarkupFile { get; set; }
    }
}
