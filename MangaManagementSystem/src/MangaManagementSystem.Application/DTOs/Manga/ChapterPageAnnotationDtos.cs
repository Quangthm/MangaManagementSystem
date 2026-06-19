using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageAnnotationDto(
        Guid ChapterPageAnnotationId,
        string IssueTypeCode,
        Guid AnnotatedByUserId,
        string? AnnotationText,
        Guid? ResolvedByUserId,
        IReadOnlyList<PageRegionDto> PageRegions,
        DateTime? CreatedAtUtc = null,
        string? AnnotatedByDisplayName = null,
        DateTime? ResolvedAtUtc = null
    );

    public record CreateChapterPageAnnotationDto(
        [Required][MaxLength(50)] string IssueTypeCode,
        [Required] Guid AnnotatedByUserId,
        string? AnnotationText,
        [Required] IReadOnlyList<Guid> PageRegionIds
    );

    public record UpdateChapterPageAnnotationDto(
        [Required] Guid ChapterPageAnnotationId,
        [Required][MaxLength(50)] string IssueTypeCode,
        [Required] Guid AnnotatedByUserId,
        string? AnnotationText,
        Guid? ResolvedByUserId,
        [Required] IReadOnlyList<Guid> PageRegionIds
    );
}
