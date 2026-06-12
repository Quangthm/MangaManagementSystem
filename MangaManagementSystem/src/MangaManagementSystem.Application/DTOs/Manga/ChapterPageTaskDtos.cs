using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageTaskDto(
        Guid ChapterPageTaskId,
        Guid AssignedToUserId,
        string TypeCode,
        string StatusCode,
        int PriorityLevel,
        DateTime? DueAtUtc,
        Guid? CompletedPageVersionId,
        IReadOnlyList<PageRegionDto> PageRegions
    );

    public record CreateChapterPageTaskDto(
        [Required] Guid AssignedToUserId,
        [Required][MaxLength(50)] string TypeCode,
        [Required][MaxLength(30)] string StatusCode,
        [Required] int PriorityLevel,
        DateTime? DueAtUtc,
        Guid? CompletedPageVersionId,
        [Required] IReadOnlyList<Guid> PageRegionIds
    );

    public record UpdateChapterPageTaskDto(
        [Required] Guid ChapterPageTaskId,
        [Required] Guid AssignedToUserId,
        [Required][MaxLength(50)] string TypeCode,
        [Required][MaxLength(30)] string StatusCode,
        [Required] int PriorityLevel,
        DateTime? DueAtUtc,
        Guid? CompletedPageVersionId,
        [Required] IReadOnlyList<Guid> PageRegionIds
    );
}
