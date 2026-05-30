using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageTaskDto(
        long ChapterPageTaskId,
        int AssignedToUserId,
        string TypeCode,
        string StatusCode,
        int PriorityLevel,
        DateTime? DueAtUtc,
        long? CompletedPageVersionId
    );

    public record CreateChapterPageTaskDto(
        [Required] int AssignedToUserId,
        [Required][MaxLength(50)] string TypeCode,
        [Required][MaxLength(30)] string StatusCode,
        [Required] int PriorityLevel,
        DateTime? DueAtUtc,
        long? CompletedPageVersionId
    );

    public record UpdateChapterPageTaskDto(
        [Required] long ChapterPageTaskId,
        [Required] int AssignedToUserId,
        [Required][MaxLength(50)] string TypeCode,
        [Required][MaxLength(30)] string StatusCode,
        [Required] int PriorityLevel,
        DateTime? DueAtUtc,
        long? CompletedPageVersionId
    );
}
