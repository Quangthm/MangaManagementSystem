using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Editor
{
    /// <summary>
    /// Aggregated read model for the Tantou Editor Chapter Review Queue page. Contains KPI
    /// summary counts and a filtered list of chapter rows. All values are sourced from the
    /// database via EF AsNoTracking reads — no mock data.
    /// </summary>
    public sealed record EditorChapterReviewQueueDto(
        int UnderReviewCount,
        int ApprovedThisWeekCount,
        int RevisionRequestedCount,
        int OnHoldCount,
        IReadOnlyList<EditorChapterReviewQueueItemDto> Chapters);

    /// <summary>
    /// A single chapter row for the review queue table. Carries the series slug and chapter id
    /// so the UI can construct a workspace URL:
    /// <c>/series/{SeriesSlug}/workspace?chapterId={ChapterId}</c>
    /// </summary>
    public sealed record EditorChapterReviewQueueItemDto(
        Guid ChapterId,
        Guid SeriesId,
        string SeriesTitle,
        string? SeriesSlug,
        string ChapterNumberLabel,
        string? ChapterTitle,
        string StatusCode,
        int PageCount,
        DateTime CreatedAtUtc,
        string? WorkspaceUrl);
}
