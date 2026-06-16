using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageVersionDto(
        Guid ChapterPageVersionId,
        Guid ChapterPageId,
        short VersionNo,
        Guid PageFileId,
        string? VersionNote,
        bool IsCurrentVersion
    );

    public record CreateChapterPageVersionDto(
        [Required] Guid ChapterPageId,
        [Required] short VersionNo,
        [Required] Guid PageFileId,
        string? VersionNote
    );

    public record UpdateChapterPageVersionDto(
        [Required] Guid ChapterPageVersionId,
        [Required] Guid ChapterPageId,
        [Required] short VersionNo,
        [Required] Guid PageFileId,
        string? VersionNote,
        [Required] bool IsCurrentVersion
    );
}
