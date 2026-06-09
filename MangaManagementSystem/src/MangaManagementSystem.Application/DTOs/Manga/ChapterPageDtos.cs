using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageDto(
        Guid ChapterPageId,
        Guid ChapterId,
        int PageNo,
        string? PageNotes,
        DateTime? DeletedAtUtc,
        Guid? DeletedByUserId
    );

    public record CreateChapterPageDto(
        [Required] Guid ChapterId,
        [Required] int PageNo,
        string? PageNotes
    );

    public record UpdateChapterPageDto(
        [Required] Guid ChapterPageId,
        [Required] Guid ChapterId,
        [Required] int PageNo,
        string? PageNotes
    );
}
