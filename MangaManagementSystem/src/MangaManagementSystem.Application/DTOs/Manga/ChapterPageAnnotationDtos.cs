using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageAnnotationDto(
        Guid ChapterPageAnnotationId,
        Guid PageRegionId,
        string IssueTypeCode,
        Guid AnnotatedByUserId,
        string? AnnotationText,
        Guid? ResolvedByUserId
    );

    public record CreateChapterPageAnnotationDto(
        [Required] Guid PageRegionId,
        [Required][MaxLength(50)] string IssueTypeCode,
        [Required] Guid AnnotatedByUserId,
        string? AnnotationText
    );

    public record UpdateChapterPageAnnotationDto(
        [Required] Guid ChapterPageAnnotationId,
        [Required] Guid PageRegionId,
        [Required][MaxLength(50)] string IssueTypeCode,
        [Required] Guid AnnotatedByUserId,
        string? AnnotationText,
        Guid? ResolvedByUserId
    );
}
