using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public sealed record QuickSelectChapterDto(
        Guid ChapterId,
        int ChapterNumber,
        string? Title,
        string? StatusCode,
        int? PageCount
    );

    public sealed record QuickSelectPageDto(
        Guid ChapterPageId,
        Guid CurrentChapterPageVersionId,
        int PageNo,
        short VersionNo
    );

    public sealed record QuickSelectAssistantDto(
        Guid UserId,
        string DisplayName,
        string? Username,
        string? Email
    );

    public sealed record QuickSelectTaskAssignmentRequest(
        Guid SeriesId,
        Guid ChapterId,
        Guid AssignedToUserId,
        string TypeCode,
        string TaskTitlePrefix,
        string DefaultTaskDescription,
        byte PriorityLevel,
        DateTime DueAtUtc,
        decimal CompensationAmount,
        IReadOnlyList<QuickSelectPageTaskRequest> Pages
    );

    public sealed record QuickSelectPageTaskRequest(
        Guid ChapterPageId,
        Guid ChapterPageVersionId,
        string? DescriptionOverride
    );

    public sealed record QuickSelectTaskAssignmentResult(
        int CreatedTaskCount,
        IReadOnlyList<QuickSelectCreatedTaskDto> CreatedTasks
    );

    public sealed record QuickSelectCreatedTaskDto(
        Guid ChapterPageTaskId,
        Guid ChapterPageId,
        Guid ChapterPageVersionId,
        int PageNo
    );
}
