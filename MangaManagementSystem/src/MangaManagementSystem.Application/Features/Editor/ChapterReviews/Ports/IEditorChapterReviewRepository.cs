using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Ports;

/// <summary>
/// Repository for the Tantou Editor Chapter Review Queue. Read queries use EF
/// <c>AsNoTracking</c>. Write operations use EF transactions.
/// </summary>
public partial interface IEditorChapterReviewRepository
{
    /// <summary>
    /// Returns KPI counts and a filtered chapter list for the review queue page.
    /// </summary>
    Task<EditorChapterReviewData> GetReviewQueueAsync(
        string? statusFilter,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the scoped review detail for one chapter, or null when it is unavailable
    /// to the acting editor.
    /// </summary>
    Task<EditorChapterReviewDetail?> GetReviewDetailForEditorAsync(
        Guid chapterId,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates an editorial review and updates the chapter status atomically.
    /// </summary>
    Task<ChapterEditorialReviewResult> SubmitChapterEditorialReviewAsync(
        Guid actorUserId,
        Guid chapterId,
        string decisionCode,
        string? comments,
        UploadedFileMetadata? markup,
        CancellationToken ct = default);

    Task<IReadOnlyList<EditorActionableChapterData>> GetActionableChaptersAsync(
        Guid actorUserId,
        Guid? seriesId,
        string? searchText,
        string? statusCode,
        int maxResults,
        CancellationToken ct = default);
}
