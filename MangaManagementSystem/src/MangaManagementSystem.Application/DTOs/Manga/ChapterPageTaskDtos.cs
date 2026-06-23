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
        int? PageVersionNo = null,
        string? PageImageUrl = null,
        decimal? CompensationAmount = null,
        string? AssignedUsername = null,
        // Mangaka review context fields
        string? CompletedOutputUrl = null,
        string? CreatedByDisplayName = null,
        DateTime? CreatedAtUtc = null,
        DateTime? UpdatedAtUtc = null,
        // Workspace link fields (read-model only, no DB change)
        string? SeriesSlug = null,
        Guid? ChapterId = null,
        Guid? SourceChapterPageVersionId = null
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

    /// <summary>
    /// Eligible assistant for task reassignment dropdown.
    /// </summary>
    public sealed record EligibleAssistantDto(
        Guid UserId,
        string DisplayName,
        string? Username
    );

    /// <summary>
    /// Request to reassign a task to a different assistant.
    /// </summary>
    public sealed record ReassignChapterPageTaskRequest(
        [Required] Guid NewAssignedToUserId,
        [Required][MaxLength(500)] string Reason,
        string? UpdatedTaskDescription
    );

    /// <summary>
    /// Result of task reassignment.
    /// </summary>
    public sealed record ReassignChapterPageTaskResult(
        Guid OldChapterPageTaskId,
        Guid NewChapterPageTaskId
    );
}
