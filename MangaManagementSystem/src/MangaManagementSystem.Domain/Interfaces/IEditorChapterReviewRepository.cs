using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    /// <summary>
    /// Repository for the Tantou Editor Chapter Review Queue. Read queries use EF
    /// <c>AsNoTracking</c>. Write operations use EF transactions.
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

        /// <summary>
        /// Creates a <c>ChapterEditorialReview</c> and updates the chapter status in one EF
        /// transaction. Validates actor permission, chapter state, and required comments before
        /// persisting.
        /// </summary>
        Task<ChapterEditorialReviewResult> SubmitChapterEditorialReviewAsync(
            Guid actorUserId,
            Guid chapterId,
            string decisionCode,
            string? comments,
            UploadedFileMetadata? markup,
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
        bool IsResolved,
        int? PageNumber,
        Guid? CurrentVersionId,
        short? CurrentVersionNo);

    /// <summary>
    /// Result of a chapter editorial review decision write operation.
    /// </summary>
    public sealed record ChapterEditorialReviewResult(
        Guid ChapterId,
        string StatusCode,
        Guid ReviewId,
        string DecisionCode,
        string? Comments,
        DateTime ReviewedAtUtc);

    /// <summary>
    /// Metadata for an uploaded file ready to be persisted as a <c>FileResource</c>.
    /// </summary>
    public sealed record UploadedFileMetadata(
        string OriginalFileName,
        string PublicId,
        string SecureUrl,
        string ContentType,
        long FileSizeBytes,
        string? Sha256Hash);
}
