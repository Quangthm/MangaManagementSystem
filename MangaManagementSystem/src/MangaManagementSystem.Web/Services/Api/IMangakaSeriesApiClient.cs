using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// Typed Web-to-API client for Mangaka series workflows. Centralizes multipart request
    /// construction and safe error parsing so Razor components do not touch HttpClient directly.
    /// </summary>
    public interface IMangakaSeriesApiClient
    {
        /// <summary>
        /// Creates a new series draft (status PROPOSAL_DRAFT) with an optional cover image.
        /// </summary>
        Task<SeriesDraftCreatedDto> CreateDraftAsync(
            Guid actorUserId,
            string title,
            string synopsis,
            string genre,
            string? contentLanguageCode = null,
            string? slug = null,
            string? publicationFrequencyCode = null,
            Guid? sourceSeriesId = null,
            byte[]? coverFileBytes = null,
            string? coverFileName = null,
            string? coverContentType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Submits an existing PROPOSAL_DRAFT series for editorial review (BF-SERIES-003).
        /// Requires a proposal document file (PDF/DOC/DOCX, max 10 MB).
        /// On success the series transitions to UNDER_EDITORIAL_REVIEW and normal draft
        /// editing is locked. Returns the created SeriesProposal identifiers and status codes.
        /// </summary>
        Task<SeriesProposalSubmittedDto> SubmitProposalAsync(
            Guid actorUserId,
            Guid seriesId,
            byte[] proposalFileBytes,
            string proposalFileName,
            string proposalContentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a PROPOSAL_DRAFT series profile (BF-SERIES-002).
        /// Cover image is optional — omit to keep the existing cover.
        /// Cover editing is locked once the series leaves PROPOSAL_DRAFT.
        /// Returns the updated profile data including the new cover URL if a cover was replaced.
        /// </summary>
        Task<SeriesDraftUpdatedDto> UpdateDraftAsync(
            Guid actorUserId,
            Guid seriesId,
            string title,
            string synopsis,
            string genre,
            string contentLanguageCode,
            string? publicationFrequencyCode = null,
            string? slug = null,
            byte[]? coverFileBytes = null,
            string? coverFileName = null,
            string? coverContentType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a PROPOSAL_DRAFT series (soft workflow transition to CANCELLED).
        /// Posts to POST /api/mangaka/series/{seriesId}/draft-cancellations.
        /// Reason is optional; pass null to cancel without a reason.
        /// </summary>
        Task<SeriesDraftCancelledDto> CancelDraftAsync(
            Guid actorUserId,
            Guid seriesId,
            string? reason = null,
            CancellationToken cancellationToken = default);
    }
}
