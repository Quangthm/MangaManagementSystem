using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterDto(
        Guid ChapterId,
        Guid SeriesId,
        string ChapterNumberLabel,
        string? ChapterTitle,
        string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc,
        DateTime CreatedAtUtc,
        Guid? CreatedByUserId
    );

    public record CreateChapterDto(
        [Required] Guid SeriesId,
        [Required][MaxLength(20)] string ChapterNumberLabel,
        [MaxLength(100)] string? ChapterTitle,
        [MaxLength(50)] string StatusCode,
        DateTime? PlannedReleaseDate
    );

    public record UpdateChapterDto(
        [Required] Guid ChapterId,
        [Required] Guid SeriesId,
        [Required][MaxLength(20)] string ChapterNumberLabel,
        [MaxLength(100)] string? ChapterTitle,
        [MaxLength(50)] string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc
    );
}
