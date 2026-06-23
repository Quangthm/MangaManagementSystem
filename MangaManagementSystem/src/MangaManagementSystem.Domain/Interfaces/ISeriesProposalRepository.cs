using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface ISeriesProposalRepository : IGenericRepository<SeriesProposal>
    {
        Task<SeriesProposal?> GetByIdWithDetailsAsync(Guid seriesProposalId, CancellationToken ct = default);
        Task<SeriesProposal?> GetLatestBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);

        /// <summary>
        /// Returns all proposals for the given series IDs, ordered by ProposalVersionNo desc then
        /// SubmittedAtUtc desc. Callers group in memory to resolve the latest per series. This
        /// avoids thread-unsafe parallel EF queries on the same DbContext.
        /// Returns an empty list when seriesIds is null or empty.
        /// </summary>
        Task<IReadOnlyList<SeriesProposal>> GetLatestForSeriesBatchAsync(
            IReadOnlyList<Guid> seriesIds, CancellationToken ct = default);
        Task<List<SeriesProposal>> GetEditorialQueueAsync(string? statusCode, Guid? seriesId, Guid? submittedByUserId, Guid? reviewedByUserId, CancellationToken ct = default);

        /// <summary>
        /// Returns all proposals for series where the specified actor is an active Mangaka
        /// contributor. Scoped by SeriesContributor membership (EndDate IS NULL, User ACTIVE,
        /// Role Mangaka). Eagerly loads Series, SubmittedByUser, ReviewedByUser, ProposalFile,
        /// and MarkupFile. Read-only EF query; no stored procedure required.
        /// </summary>
        Task<IReadOnlyList<SeriesProposal>> GetMySeriesProposalsAsync(Guid actorUserId, CancellationToken ct = default);

        /// <summary>
        /// Returns true when the specified user is an active Tantou Editor contributor of the
        /// given series (SeriesContributor.EndDate IS NULL, User ACTIVE, Role 'Tantou Editor').
        /// This mirrors the membership predicate used by the editorial-review stored procedures
        /// and represents the "claimed" state for editorial review. Read-only EF query.
        /// </summary>
        Task<bool> IsActiveTantouEditorContributorAsync(Guid seriesId, Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Submits a series proposal for editorial review via <c>manga.usp_SeriesProposal_Submit</c>.
        /// The stored procedure: validates the series is PROPOSAL_DRAFT, validates the submitter is
        /// an active Mangaka contributor, creates the SERIES_PROPOSAL FileResource, creates the
        /// SeriesProposal row, transitions Series to UNDER_EDITORIAL_REVIEW, and writes the audit event.
        /// The caller must NOT pass title/synopsis/genre — the procedure snapshots them from Series.
        /// All file metadata parameters are required (no nulls accepted by the procedure).
        /// </summary>
        Task<(Guid SeriesProposalId, short ProposalVersionNo)> SubmitSeriesProposalViaProcAsync(
            Guid seriesId,
            Guid submittedByUserId,
            string originalFileName,
            string cloudinaryPublicId,
            string cloudinarySecureUrl,
            string contentType,
            long fileSizeBytes,
            string sha256Hash,
            CancellationToken cancellationToken = default);

        Task<Guid?> ClaimEditorialReviewAsync(Guid seriesProposalId, Guid actorUserId, string? notes, CancellationToken ct = default);
        
        Task<Guid?> RequestRevisionAsync(Guid seriesProposalId, Guid actorUserId, string comments, 
            string? markupOriginalFileName = null, string? markupCloudinaryPublicId = null, string? markupCloudinarySecureUrl = null, 
            string? markupContentType = null, long? markupFileSizeBytes = null, string? markupSha256Hash = null, CancellationToken ct = default);
            
        Task<Guid?> PassToBoardAsync(Guid seriesProposalId, Guid actorUserId, string? comments, 
            string? markupOriginalFileName = null, string? markupCloudinaryPublicId = null, string? markupCloudinarySecureUrl = null, 
            string? markupContentType = null, long? markupFileSizeBytes = null, string? markupSha256Hash = null, CancellationToken ct = default);
            
        Task<Guid> CancelProposalAsync(Guid seriesProposalId, Guid actorUserId, string comments, 
            string markupOriginalFileName, string markupCloudinaryPublicId, string markupCloudinarySecureUrl, 
            string markupContentType, long markupFileSizeBytes, string? markupSha256Hash = null, CancellationToken ct = default);
    }
}
