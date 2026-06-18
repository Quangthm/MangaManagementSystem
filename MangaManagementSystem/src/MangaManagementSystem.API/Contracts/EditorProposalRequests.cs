using Microsoft.AspNetCore.Http;

namespace MangaManagementSystem.API.Contracts
{
    /// <summary>
    /// JSON body for claiming a proposal for editorial review. Notes are optional.
    /// </summary>
    public sealed class ClaimProposalRequest
    {
        public string? Notes { get; init; }
    }

    /// <summary>
    /// Multipart/form-data contract for the Request Revision editorial decision.
    /// Comments are required; the markup file is optional. The controller reads bytes and
    /// forwards them to MediatR; IFormFile never leaves the API boundary.
    /// </summary>
    public sealed class RequestRevisionForm
    {
        public string? Comments { get; init; }
        public IFormFile? MarkupFile { get; init; }
    }

    /// <summary>
    /// Multipart/form-data contract for the Pass To Board editorial decision.
    /// Comments and markup file are both optional.
    /// </summary>
    public sealed class PassToBoardForm
    {
        public string? Comments { get; init; }
        public IFormFile? MarkupFile { get; init; }
    }

    /// <summary>
    /// Multipart/form-data contract for the Cancel Proposal editorial decision.
    /// Comments are required and the markup file is required.
    /// </summary>
    public sealed class CancelProposalForm
    {
        public string? Comments { get; init; }
        public IFormFile? MarkupFile { get; init; }
    }
}
