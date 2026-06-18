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
        string TaskTitle,
        string TaskDescription,
        IReadOnlyList<PageRegionDto> PageRegions,
        Guid? SeriesId = null,
        string? AssignedToDisplayName = null,
        // Optional context fields for Assistant read/detail
        string? SeriesTitle = null,
        string? ChapterNumberLabel = null,
        string? ChapterTitle = null,
        int? PageNo = null,
        string? PageImageUrl = null,
        decimal? CompensationAmount = null,
        string? AssignedUsername = null
    );

    public record CreateChapterPageTaskDto(
        [Required] Guid ActorUserId,
        [Required] Guid AssignedToUserId,
        [Required][MaxLength(50)] string TypeCode,
        [Required][MaxLength(30)] string StatusCode,
        [Required][MaxLength(200)] string TaskTitle,
        [Required] string TaskDescription,
        [Required] int PriorityLevel,
        DateTime? DueAtUtc,
        decimal? CompensationAmount,
        Guid? CompletedPageVersionId,
        [Required] IReadOnlyList<Guid> PageRegionIds
    );

    public record UpdateChapterPageTaskDto(
        [Required] Guid ChapterPageTaskId,
        [Required] Guid AssignedToUserId,
        [Required][MaxLength(50)] string TypeCode,
        [Required][MaxLength(30)] string StatusCode,
        [Required][MaxLength(200)] string TaskTitle,
        [Required] string TaskDescription,
        [Required] int PriorityLevel,
        DateTime? DueAtUtc,
        decimal? CompensationAmount,
        Guid? CompletedPageVersionId,
        [Required] IReadOnlyList<Guid> PageRegionIds
    );
}
