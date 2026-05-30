using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterDto(
        long ChapterId,
        long SeriesId,
        string ChapterNumberLabel,
        string? ChapterTitle,
        string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc,
        DateTime CreatedAtUtc,
        int? CreatedByUserId
    );

    public record CreateChapterDto(
        [Required] long SeriesId,
        [Required][MaxLength(20)] string ChapterNumberLabel,
        [MaxLength(100)] string? ChapterTitle,
        [MaxLength(50)] string StatusCode,
        DateTime? PlannedReleaseDate
    );

    public record UpdateChapterDto(
        [Required] long ChapterId,
        [Required] long SeriesId,
        [Required][MaxLength(20)] string ChapterNumberLabel,
        [MaxLength(100)] string? ChapterTitle,
        [MaxLength(50)] string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc
    );
}
