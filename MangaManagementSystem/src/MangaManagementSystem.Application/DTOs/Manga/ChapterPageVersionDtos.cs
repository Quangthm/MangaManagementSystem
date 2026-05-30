using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageVersionDto(
        long ChapterPageVersionId,
        long ChapterPageId,
        short VersionNo,
        long PageFileId,
        string? VersionNote,
        bool IsCurrentVersion
    );

    public record CreateChapterPageVersionDto(
        [Required] long ChapterPageId,
        [Required] short VersionNo,
        [Required] long PageFileId,
        string? VersionNote
    );

    public record UpdateChapterPageVersionDto(
        [Required] long ChapterPageVersionId,
        [Required] long ChapterPageId,
        [Required] short VersionNo,
        [Required] long PageFileId,
        string? VersionNote,
        [Required] bool IsCurrentVersion
    );
}
