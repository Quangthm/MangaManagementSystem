using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public sealed record AssistantCompletedWorkSummaryDto(
        int CompletedTaskCount,
        int ApprovedRegionCount,
        decimal TotalEstimatedAmount,
        decimal ThisMonthEstimatedAmount,
        IReadOnlyList<AssistantCompletedWorkBreakdownDto> Breakdown,
        IReadOnlyList<AssistantCompletedWorkItemDto> RecentItems
    );

    public sealed record AssistantCompletedWorkBreakdownDto(
        string TaskType,
        int CompletedTaskCount,
        int RegionCount,
        decimal EstimatedAmount
    );

    public sealed record AssistantCompletedWorkItemDto(
        Guid TaskId,
        string TaskType,
        string SeriesTitle,
        string ChapterTitle,
        int PageNumber,
        int RegionCount,
        decimal EstimatedAmount,
        DateTime? CompletedAt
    );
}
