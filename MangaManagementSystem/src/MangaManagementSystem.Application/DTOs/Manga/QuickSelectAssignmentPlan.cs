using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public sealed record QuickSelectAssignmentPlan(
        Guid ActorUserId,
        Guid SeriesId,
        Guid ChapterId,
        Guid AssignedToUserId,
        string TypeCode,
        string TaskTitlePrefix,
        byte PriorityLevel,
        DateTime DueAtUtc,
        decimal CompensationAmount,
        IReadOnlyList<QuickSelectAssignmentPlanItem> Items
    );

    public sealed record QuickSelectAssignmentPlanItem(
        Guid ChapterPageTaskId,
        Guid ChapterPageId,
        Guid ChapterPageVersionId,
        int PageNo,
        short VersionNo,
        Guid PageFileResourceId,
        string CloudinaryPublicId,
        int ImageWidth,
        int ImageHeight,
        Guid? ExistingFullPageRegionId,
        string FinalTaskTitle,
        string FinalTaskDescription,
        string? UsedDescriptionOverride
    );
}
