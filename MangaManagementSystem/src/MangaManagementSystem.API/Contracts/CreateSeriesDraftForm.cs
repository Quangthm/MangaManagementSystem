using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MangaManagementSystem.API.Contracts
{
    /// <summary>
    /// multipart/form-data payload for creating a new series draft.
    /// Carries draft fields plus an optional series cover file. It intentionally does NOT
    /// carry a proposal document — proposal submission is a separate later workflow.
    /// </summary>
    public class CreateSeriesDraftForm
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Synopsis { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Genre { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? ContentLanguageCode { get; set; }

        [MaxLength(220)]
        public string? Slug { get; set; }

        [MaxLength(50)]
        public string? PublicationFrequencyCode { get; set; }

        public Guid? SourceSeriesId { get; set; }

        /// <summary>
        /// Optional series cover image. When present it is stored with purpose SERIES_COVER.
        /// </summary>
        public IFormFile? CoverFile { get; set; }
    }
}
