using System;
using System.Collections.Generic;
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
        /// Returns series where the logged-in actor is an active Mangaka contributor.
        /// Server-side scoped — only series the actor contributes to are returned.
        /// </summary>
        Task<IReadOnlyList<SeriesDto>> GetMySeriesAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new series draft (status PROPOSAL_DRAFT) with an optional cover image.
        /// </summary>
        Task<SeriesDraftCreatedDto> CreateDraftAsync(
            Guid actorUserId,
            string title,
            string synopsis,
            IReadOnlyList<Guid> genreIds,
            IReadOnlyList<Guid> tagIds,
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
            IReadOnlyList<Guid> genreIds,
            IReadOnlyList<Guid> tagIds,
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

        /// <summary>
        /// Returns all series proposals scoped to the actor's active Mangaka contributor
        /// memberships. Read-only tracking — no mutations. Server-side scoped.
        /// </summary>
        Task<IReadOnlyList<MangakaSeriesProposalDto>> GetMySeriesProposalsAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a single proposal by ID, scoped to the actor's active Mangaka contributor
        /// memberships. Returns null when not found or not authorized. Read-only.
        /// </summary>
        Task<MangakaSeriesProposalDto?> GetMySeriesProposalDetailAsync(
            Guid actorUserId,
            Guid proposalId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a single series card by id where the actor is an active Mangaka contributor.
        /// Same scoping as GetMySeriesAsync but targeted. Returns null when not found or not authorized.
        /// </summary>
        Task<SeriesDto?> GetMySeriesCardByIdAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default);
    }
}
