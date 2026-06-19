using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    /// <summary>
    /// Read-only repository for the Tantou Editor Chapter Review Queue. All queries use EF
    /// <c>AsNoTracking</c>. No writes or stored-procedure transitions live here. Returns
    /// Domain records and primitive counts — DTO shaping happens in the Application handler.
    /// </summary>
    public interface IEditorChapterReviewRepository
    {
        /// <summary>
        /// Returns KPI counts and a filtered chapter list for the review queue page.
        /// <paramref name="statusFilter"/> narrows the chapter list; null/empty/"all" means
        /// all reviewable statuses (UNDER_REVIEW, REVISION_REQUESTED, ON_HOLD). Each chapter
        /// carries its page count (derived from the ChapterPages table) and its parent Series
        /// (with Slug) so the handler can build workspace URLs.
        ///
        /// Scope: only chapters belonging to series where <paramref name="actorUserId"/> is an
        /// active Tantou Editor contributor are counted and listed.
        /// </summary>
        Task<EditorChapterReviewData> GetReviewQueueAsync(
            string? statusFilter,
            Guid actorUserId,
            CancellationToken ct = default);

        /// <summary>
        /// Returns the scoped review detail for one chapter, or null when the chapter does not
        /// exist or <paramref name="actorUserId"/> is not an active Tantou Editor contributor of
        /// the chapter's series. Returning null lets the API respond 403/not-found without
        /// leaking chapter or series details.
        /// </summary>
        Task<EditorChapterReviewDetail?> GetReviewDetailForEditorAsync(
            Guid chapterId,
            Guid actorUserId,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Aggregated chapter review queue read result.
    /// </summary>
    public sealed record EditorChapterReviewData(
        int UnderReviewCount,
        int ApprovedThisWeekCount,
        int RevisionRequestedCount,
        int OnHoldCount,
        IReadOnlyList<EditorChapterReviewChapter> Chapters);

    /// <summary>
    /// A single chapter row enriched with its page count and parent series. The series is
    /// eagerly loaded so the handler can access Title/Slug without a separate query.
    /// </summary>
    public sealed record EditorChapterReviewChapter(
        Guid ChapterId,
        Guid SeriesId,
        string ChapterNumberLabel,
        string? ChapterTitle,
        string StatusCode,
        int PageCount,
        DateTime CreatedAtUtc,
        Series? Series);

    /// <summary>
    /// Scoped chapter review detail read result. Pages and open annotations are pre-shaped
    /// into primitive-friendly records so the Application handler can map straight to DTOs
    /// without further EF access.
    /// </summary>
    public sealed record EditorChapterReviewDetail(
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
        IReadOnlyList<EditorChapterReviewDetailPage> Pages,
        IReadOnlyList<EditorChapterReviewDetailAnnotation> OpenAnnotations);

    public sealed record EditorChapterReviewDetailPage(
        Guid ChapterPageId,
        int PageNumber,
        Guid? CurrentVersionId,
        string? CurrentVersionFileUrl,
        short? CurrentVersionNo);

    public sealed record EditorChapterReviewDetailAnnotation(
        Guid AnnotationId,
        string Comment,
        string IssueTypeCode,
        DateTime CreatedAtUtc,
        string? CreatedByDisplayName,
        bool IsResolved);
}
