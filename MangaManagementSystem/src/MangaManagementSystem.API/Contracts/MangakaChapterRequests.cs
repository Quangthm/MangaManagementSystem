using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public sealed class CreateChapterDraftApiRequest
    {
        [Required]
        public Guid SeriesId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ChapterNumberLabel { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ChapterTitle { get; set; }
    }

    public sealed class UpdateChapterDraftApiRequest
    {
        [Required]
        [MaxLength(20)]
        public string ChapterNumberLabel { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ChapterTitle { get; set; }
    }

    public sealed class ScheduleApprovedChapterApiRequest
    {
        [Required]
        public DateTime PlannedReleaseDate { get; set; }
    }
}
