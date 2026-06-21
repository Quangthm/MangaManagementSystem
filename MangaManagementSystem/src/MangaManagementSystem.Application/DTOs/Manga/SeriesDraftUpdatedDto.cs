using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Result returned after a successful Edit Series Draft Profile workflow (BF-SERIES-002).
    /// Returns the updated series identity and the new cover FileResource id when a cover
    /// was replaced during the update.
    /// </summary>
    public sealed class SeriesDraftUpdatedDto
    {
        public Guid SeriesId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public IReadOnlyList<GenreDto> Genres { get; init; } = new List<GenreDto>();
        public IReadOnlyList<TagDto> Tags { get; init; } = new List<TagDto>();
        public string Synopsis { get; init; } = string.Empty;
        public string ContentLanguageCode { get; init; } = string.Empty;
        public string? PublicationFrequencyCode { get; init; }
        /// <summary>
        /// New cover FileResource id when cover was replaced; null if cover was not changed.
        /// </summary>
        public Guid? NewCoverFileResourceId { get; init; }
        /// <summary>
        /// Cloudinary secure URL of the new cover, when a new cover was uploaded.
        /// Null if cover was not changed. Used by the Web to update the in-memory card.
        /// </summary>
        public string? NewCoverUrl { get; init; }
    }
}
