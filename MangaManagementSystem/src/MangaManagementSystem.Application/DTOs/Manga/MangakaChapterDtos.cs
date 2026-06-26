using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Mangaka chapter list item with latest editorial review summary.
    /// </summary>
    public sealed record MangakaChapterListItemDto(
        Guid ChapterId,
        Guid SeriesId,
        string SeriesTitle,
        string ChapterNumberLabel,
        string? ChapterTitle,
        string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        ChapterEditorialReviewSummaryDto? LatestReview);

    /// <summary>
    /// Summary of the latest chapter editorial review decision.
    /// </summary>
    public sealed record ChapterEditorialReviewSummaryDto(
        Guid ChapterEditorialReviewId,
        string ReviewerDisplayName,
        string DecisionCode,
        string? Comments,
        Guid? MarkupFileId,
        string? MarkupFileUrl,
        DateTime ReviewedAtUtc);

    /// <summary>
    /// Request to create a new chapter draft.
    /// </summary>
    public sealed record CreateChapterDraftRequest(
        Guid SeriesId,
        string ChapterNumberLabel,
        string? ChapterTitle);

    /// <summary>
    /// Request to update chapter draft metadata.
    /// </summary>
    public sealed record UpdateChapterDraftRequest(
        string ChapterNumberLabel,
        string? ChapterTitle);

    /// <summary>
    /// Request to schedule an approved chapter with a planned release date.
    /// </summary>
    public sealed record ScheduleApprovedChapterRequest(
        DateTime PlannedReleaseDate);
}
