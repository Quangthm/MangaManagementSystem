using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// Typed Web-to-API client for Tantou Editor series-proposal review. Centralizes JSON and
    /// multipart request construction plus safe error parsing so Razor components never touch
    /// HttpClient, Cloudinary, or Application services directly.
    /// </summary>
    public interface IEditorProposalApiClient
    {
        /// <summary>
        /// Returns the editorial review queue, optionally filtered by proposal status.
        /// Pass null/empty status for all proposals.
        /// </summary>
        Task<IReadOnlyList<ProposalQueueItemDto>> GetQueueAsync(
            Guid actorUserId,
            string? statusCode = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a single proposal's read-only detail with computed permission flags.
        /// Returns null when the proposal is not found.
        /// </summary>
        Task<EditorProposalDetailDto?> GetDetailAsync(
            Guid actorUserId,
            Guid proposalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Claims a proposal for editorial review. Notes are optional.
        /// </summary>
        Task<EditorReviewActionResultDto> ClaimAsync(
            Guid actorUserId,
            Guid proposalId,
            string? notes = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a Request Revision decision. Comments required; markup optional.
        /// </summary>
        Task<EditorReviewActionResultDto> RequestRevisionAsync(
            Guid actorUserId,
            Guid proposalId,
            string comments,
            byte[]? markupFileBytes = null,
            string? markupFileName = null,
            string? markupContentType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a Pass To Board decision. Comments and markup both optional. Moves the
        /// proposal to UNDER_BOARD_REVIEW only — never APPROVED.
        /// </summary>
        Task<EditorReviewActionResultDto> PassToBoardAsync(
            Guid actorUserId,
            Guid proposalId,
            string? comments = null,
            byte[]? markupFileBytes = null,
            string? markupFileName = null,
            string? markupContentType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a Cancel Proposal decision. Comments and markup file are both required.
        /// </summary>
        Task<EditorReviewActionResultDto> CancelAsync(
            Guid actorUserId,
            Guid proposalId,
            string comments,
            byte[] markupFileBytes,
            string markupFileName,
            string markupContentType,
            CancellationToken cancellationToken = default);
    }
}
