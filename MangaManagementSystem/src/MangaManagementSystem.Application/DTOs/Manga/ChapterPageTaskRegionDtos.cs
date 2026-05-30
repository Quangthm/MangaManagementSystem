using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageTaskRegionDto(
        long ChapterPageTaskRegionId,
        long ChapterPageTaskId,
        long PageRegionId
    );

    public record CreateChapterPageTaskRegionDto(
        [Required] long ChapterPageTaskId,
        [Required] long PageRegionId
    );

    public record UpdateChapterPageTaskRegionDto(
        [Required] long ChapterPageTaskRegionId,
        [Required] long ChapterPageTaskId,
        [Required] long PageRegionId
    );
}
