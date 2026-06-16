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
    }
}
