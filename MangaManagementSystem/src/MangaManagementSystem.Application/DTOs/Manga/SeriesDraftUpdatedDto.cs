using System;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Result returned after a successful Edit Series Draft Profile workflow (BF-SERIES-002).
    /// Returns the updated series identity and the new cover FileResource id when a cover
    /// was replaced during the update.
    ///
    /// This is a minimal command result following Option B CQRS: the command mutates state;
    /// the Web reloads the full read model separately via LoadSeriesAsync() to get fresh
    /// Genres, Tags, UpdatedAtUtc, CoverUrl, etc.
    /// </summary>
    public sealed class SeriesDraftUpdatedDto
    {
        public Guid SeriesId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
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
