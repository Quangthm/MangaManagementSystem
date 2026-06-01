using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageTaskRegionDto(
        long ChapterPageTaskId,
        long PageRegionId
    );

    public record CreateChapterPageTaskRegionDto(
        [Required] long ChapterPageTaskId,
        [Required] long PageRegionId
    );

    public record UpdateChapterPageTaskRegionDto(
        [Required] long KeyId,
        [Required] long ChapterPageTaskId,
        [Required] long PageRegionId
    );
}
