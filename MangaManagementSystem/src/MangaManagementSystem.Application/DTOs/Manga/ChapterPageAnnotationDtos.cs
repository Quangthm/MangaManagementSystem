using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageAnnotationDto(
        long ChapterPageAnnotationId,
        long PageRegionId,
        string IssueTypeCode,
        int AnnotatedByUserId,
        string? AnnotationText,
        int? ResolvedByUserId
    );

    public record CreateChapterPageAnnotationDto(
        [Required] long PageRegionId,
        [Required][MaxLength(50)] string IssueTypeCode,
        [Required] int AnnotatedByUserId,
        string? AnnotationText
    );

    public record UpdateChapterPageAnnotationDto(
        [Required] long ChapterPageAnnotationId,
        [Required] long PageRegionId,
        [Required][MaxLength(50)] string IssueTypeCode,
        [Required] int AnnotatedByUserId,
        string? AnnotationText,
        int? ResolvedByUserId
    );
}
