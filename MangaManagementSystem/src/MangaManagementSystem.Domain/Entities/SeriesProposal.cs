using System;

namespace MangaManagementSystem.Domain.Entities
{
public class SeriesProposal
{
    public Guid SeriesProposalId { get; set; }
    public Guid SeriesId { get; set; }
    public Series? Series { get; set; }
    public short ProposalVersionNo { get; set; }
    public string ProposalTitle { get; set; } = null!;
    public string SynopsisSnapshot { get; set; } = null!;
    public string GenreSnapshot { get; set; } = null!;
    public Guid ProposalFileId { get; set; }
    public FileResource? ProposalFile { get; set; }
    public string StatusCode { get; set; } = "UNDER_EDITORIAL_REVIEW";
    public Guid SubmittedByUserId { get; set; }
    public User? SubmittedByUser { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? WithdrawnAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string? Comments { get; set; }
        public Guid? MarkupFileId { get; set; }
        public FileResource? MarkupFile { get; set; }
    }
}
