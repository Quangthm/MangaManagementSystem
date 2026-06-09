using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageTaskRegionDto(
        Guid ChapterPageTaskId,
        Guid PageRegionId
    );

    public record CreateChapterPageTaskRegionDto(
        [Required] Guid ChapterPageTaskId,
        [Required] Guid PageRegionId
    );

    public record UpdateChapterPageTaskRegionDto(
        [Required] Guid KeyId,
        [Required] Guid ChapterPageTaskId,
        [Required] Guid PageRegionId
    );
}
