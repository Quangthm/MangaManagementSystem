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
