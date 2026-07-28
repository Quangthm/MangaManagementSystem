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
        /// avoids parallel data access during batch resolution.
        /// Returns an empty list when seriesIds is null or empty.
        /// </summary>
        Task<IReadOnlyList<SeriesProposal>> GetLatestForSeriesBatchAsync(
            IReadOnlyList<Guid> seriesIds, CancellationToken ct = default);
        Task<List<SeriesProposal>> GetEditorialQueueAsync(string? statusCode, Guid? seriesId, Guid? submittedByUserId, Guid? reviewedByUserId, CancellationToken ct = default);

        /// <summary>
        /// Returns all proposals for series where the specified actor is an active Mangaka
        /// contributor. Scoped by SeriesContributor membership (EndDate IS NULL, User ACTIVE,
        /// Role Mangaka). Eagerly loads Series, SubmittedByUser, ReviewedByUser, ProposalFile,
        /// and MarkupFile. Read-only query.
        /// </summary>
        Task<IReadOnlyList<SeriesProposal>> GetMySeriesProposalsAsync(Guid actorUserId, CancellationToken ct = default);

        /// <summary>
        /// Returns a single proposal by ID, scoped to the specified actor's active Mangaka
        /// contributor memberships. Returns null when not found or not authorized.
        /// Same eager includes as GetMySeriesProposalsAsync. Read-only query.
        /// </summary>
        Task<SeriesProposal?> GetMySeriesProposalDetailAsync(Guid actorUserId, Guid seriesProposalId, CancellationToken ct = default);

        /// <summary>
        /// Returns true when the specified user is an active Tantou Editor contributor of the
        /// given series (SeriesContributor.EndDate IS NULL, User ACTIVE, Role 'Tantou Editor').
        /// This mirrors the membership predicate used by editorial review workflows
        /// and represents the "claimed" state for editorial review. Read-only query.
        /// </summary>
        Task<bool> IsActiveTantouEditorContributorAsync(Guid seriesId, Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Returns all active Tantou Editor contributors for the given series.
        /// Active = SeriesContributor.EndDate IS NULL, User ACTIVE, Role 'Tantou Editor'.
        /// Read-only query.
        /// </summary>
        Task<IReadOnlyList<ActiveTantouEditorInfo>> GetActiveTantouEditorContributorsAsync(
            Guid seriesId, CancellationToken ct = default);

        /// <summary>
        /// Submits a series proposal for editorial review.
        /// Validates that the series is a proposal draft and that the submitter
        /// is an active Mangaka contributor.
        /// Creates the proposal file and proposal version, transitions the series
        /// to editorial review, and records the corresponding audit event.
        /// Title, synopsis, genres, and tags are snapshotted from the series.
        /// </summary>
        Task<(Guid SeriesProposalId, short ProposalVersionNo)> SubmitSeriesProposalAsync(
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

    /// <summary>
    /// Read-only info for an active Tantou Editor contributor of a series.
    /// Used by proposal detail to display existing active editors.
    /// </summary>
    public sealed record ActiveTantouEditorInfo(
        Guid UserId,
        string DisplayName,
        string? Username,
        DateTime? StartedAtUtc);
}
