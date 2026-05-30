using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageDto(
        long ChapterPageId,
        long ChapterId,
        int PageNo,
        string? PageNotes,
        DateTime? DeletedAtUtc,
        int? DeletedByUserId
    );

    public record CreateChapterPageDto(
        [Required] long ChapterId,
        [Required] int PageNo,
        string? PageNotes
    );

    public record UpdateChapterPageDto(
        [Required] long ChapterPageId,
        [Required] long ChapterId,
        [Required] int PageNo,
        string? PageNotes
    );
}
