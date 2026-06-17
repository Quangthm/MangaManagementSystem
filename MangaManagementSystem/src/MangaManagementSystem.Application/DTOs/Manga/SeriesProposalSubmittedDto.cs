namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Result returned after a successful Submit Series Proposal workflow.
    /// The stored procedure transitions Series to UNDER_EDITORIAL_REVIEW and creates the
    /// SeriesProposal row; this DTO carries the identifiers and resulting status codes back
    /// to the API controller and then to the typed Web client.
    /// </summary>
    public sealed class SeriesProposalSubmittedDto
    {
        public Guid SeriesId { get; init; }
        public Guid SeriesProposalId { get; init; }
        public short ProposalVersionNo { get; init; }
        public string SeriesStatusCode { get; init; } = "UNDER_EDITORIAL_REVIEW";
        public string ProposalStatusCode { get; init; } = "UNDER_EDITORIAL_REVIEW";
    }
}
