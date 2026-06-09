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
        Guid? CreatedByUserId,
        DateTime? UpdatedAtUtc
    );

    public record CreateChapterDto(
        [Required] Guid SeriesId,
        [Required][MaxLength(20)] string ChapterNumberLabel,
        [MaxLength(200)] string? ChapterTitle,
        DateTime? PlannedReleaseDate,
        Guid? CreatedByUserId
    );

    public record UpdateChapterDto(
        [Required] Guid ChapterId,
        [Required] Guid SeriesId,
        [Required][MaxLength(20)] string ChapterNumberLabel,
        [MaxLength(200)] string? ChapterTitle,
        [Required][MaxLength(50)] string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc
    );
}