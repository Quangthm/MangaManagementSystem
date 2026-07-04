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

    /// <summary>
    /// Read-only detail model for a single chapter under editorial review. Returned only when
    /// the requesting actor is an active Tantou Editor contributor of the chapter's series;
    /// otherwise the API responds 403 and no detail is leaked.
    ///
    /// Schema limitations (documented, not invented):
    /// - Chapter has no synopsis/description column, so no such field is exposed.
    /// - There is no chapter-to-editor assignment concept, so <see cref="AssignedEditorDisplayName"/>
    ///   is always null in MVP.
    /// - Chapter has no SubmittedAtUtc/ApprovedAtUtc, so <see cref="CreatedAtUtc"/> is used.
    /// </summary>
    public sealed record EditorChapterReviewDetailDto(
        Guid ChapterId,
        Guid SeriesId,
        string SeriesTitle,
        string? SeriesSlug,
        string ChapterNumberLabel,
        string? ChapterTitle,
        string StatusCode,
        int PageCount,
        DateTime CreatedAtUtc,
        string? SubmittedByDisplayName,
        string? AssignedEditorDisplayName,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc,
        DateTime? UpdatedAtUtc,
        IReadOnlyList<EditorChapterReviewPageDto> Pages,
        IReadOnlyList<EditorChapterReviewAnnotationDto> OpenAnnotations,
        IReadOnlyList<EditorChapterReviewHistoryDto> EditorialReviewHistory,
        string? WorkspaceUrl,
        bool CanOpenWorkspace);

    /// <summary>
    /// A single chapter page with its current version reference and file URL (if a current
    /// version exists). Read-only — page editing/upload is out of scope for this page.
    /// </summary>
    public sealed record EditorChapterReviewPageDto(
        Guid ChapterPageId,
        int PageNumber,
        Guid? CurrentVersionId,
        string? CurrentVersionFileUrl,
        short? CurrentVersionNo);

    /// <summary>
    /// A read-only open (unresolved) annotation tied to the chapter via its page regions.
    /// </summary>
    public sealed record EditorChapterReviewAnnotationDto(
        Guid AnnotationId,
        string Comment,
        string IssueTypeCode,
        DateTime CreatedAtUtc,
        string? CreatedByDisplayName,
        bool IsResolved,
        int? PageNumber,
        Guid? CurrentVersionId,
        short? CurrentVersionNo);

    /// <summary>
    /// A single entry in the editorial review history for a chapter.
    /// </summary>
    public sealed record EditorChapterReviewHistoryDto(
        Guid ReviewId,
        string DecisionCode,
        string? Comments,
        DateTime ReviewedAtUtc,
        string ReviewerDisplayName,
        Guid? MarkupFileId,
        string? MarkupFileName,
        string? MarkupFileUrl,
        string? MarkupContentType,
        long? MarkupFileSizeBytes);

    /// <summary>
    /// Request body for submitting a chapter editorial review decision.
    /// </summary>
    public sealed record SubmitChapterEditorialReviewRequest(
        string DecisionCode,
        string? Comments,
        Guid? MarkupFileId = null);

    /// <summary>
    /// Response after a successful chapter editorial review decision.
    /// </summary>
    public sealed record SubmitChapterEditorialReviewResponse(
        Guid ChapterId,
        string StatusCode,
        Guid ReviewId,
        string DecisionCode,
        string? Comments,
        DateTime ReviewedAtUtc);
}
