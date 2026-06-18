using Microsoft.AspNetCore.Http;

namespace MangaManagementSystem.API.Contracts
{
    /// <summary>
    /// Multipart/form-data contract for BF-SERIES-003 — Submit Series Proposal.
    /// The proposal document file is required and must be PDF/DOC/DOCX only. The controller
    /// reads the bytes and forwards them to MediatR; business validation remains in Application.
    /// </summary>
    public sealed class SubmitSeriesProposalForm
    {
        public IFormFile? ProposalFile { get; init; }
    }
}
