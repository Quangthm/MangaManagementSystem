using MangaManagementSystem.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface ISeriesRepository : IGenericRepository<Series>
    {
        Task<Series?> GetSeriesWithChaptersAsync(Guid seriesId);

        /// <summary>
        /// Returns all series where the specified actor is an active contributor,
        /// with CoverFile eagerly loaded for dashboard display.
        /// Filters by: SeriesContributor.UserId == actorUserId, EndDate IS NULL,
        /// User.StatusCode == "ACTIVE", and Role.RoleName == "Mangaka".
        /// </summary>
        Task<IReadOnlyList<Series>> GetByActiveContributorWithCoverAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns all series with the <c>CoverFile</c> navigation property eagerly loaded.
        /// Used by the Mangaka dashboard to render cover thumbnails without a per-card N+1 query.
        /// Display-only; the URL is read from <c>FileResource.CloudinarySecureUrl</c>.
        /// Non-deleted cover files are returned; a null CloudinarySecureUrl means no cover.
        /// </summary>
        Task<IReadOnlyList<Series>> GetAllWithCoverAsync();

        /// <summary>
        /// Updates a series draft profile through <c>manga.usp_Series_UpdateProfile</c>.
        /// Only series with status <c>PROPOSAL_DRAFT</c> can be updated.
        /// The stored procedure: validates actor is active Mangaka contributor, enforces
        /// PROPOSAL_DRAFT status guard, soft-deletes the old cover FileResource when a new
        /// cover is supplied, creates a new SERIES_COVER FileResource, updates manga.Series,
        /// and writes the audit event.
        /// Cover metadata is all-or-nothing: pass all six cover params or all nulls.
        /// Returns the new cover FileResource id (null when cover was not changed).
        /// </summary>
        Task<Guid?> UpdateSeriesDraftViaProcAsync(
            Guid actorUserId,
            Guid seriesId,
            string title,
            string slug,
            string synopsis,
            string genre,
            string contentLanguageCode,
            string? publicationFrequencyCode,
            string? coverOriginalFileName,
            string? coverCloudinaryPublicId,
            string? coverCloudinarySecureUrl,
            string? coverContentType,
            long? coverFileSizeBytes,
            string? coverSha256Hash,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a PROPOSAL_DRAFT series through <c>manga.usp_Series_CancelDraft</c>.
        /// The stored procedure: validates the series is PROPOSAL_DRAFT, validates the actor
        /// is an active Mangaka contributor, transitions Series.status_code to CANCELLED,
        /// and writes the SERIES_DRAFT_CANCELLED audit event.
        /// No FileResource cleanup or Cloudinary involvement — pure status transition.
        /// </summary>
        Task CancelSeriesDraftViaProcAsync(
            Guid actorUserId,
            Guid seriesId,
            string? reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a series draft through the <c>manga.usp_Series_Create</c> stored procedure.
        /// The procedure enforces actor permission, creates the optional SERIES_COVER FileResource,
        /// inserts the Series (status PROPOSAL_DRAFT), seeds the creator contributor, and writes the audit event.
        /// Cover metadata is all-or-nothing; pass nulls when no cover is provided.
        /// Returns the new series id and the cover FileResource id (null when no cover was provided).
        /// </summary>
        Task<(Guid newSeriesId, Guid? coverFileResourceId)> CreateSeriesDraftViaProcAsync(
            Guid actorUserId,
            string title,
            string slug,
            string synopsis,
            string genre,
            string contentLanguageCode,
            Guid? sourceSeriesId,
            string? publicationFrequencyCode,
            string? coverOriginalFileName,
            string? coverCloudinaryPublicId,
            string? coverCloudinarySecureUrl,
            string? coverContentType,
            long? coverFileSizeBytes,
            string? coverSha256Hash,
            CancellationToken cancellationToken = default);
    }
}
