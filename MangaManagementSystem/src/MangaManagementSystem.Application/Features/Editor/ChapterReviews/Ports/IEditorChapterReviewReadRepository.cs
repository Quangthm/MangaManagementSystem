using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Ports;

public interface IEditorChapterReviewReadRepository
{
    Task<EditorChapterReviewData> GetReviewQueueAsync(
        string? statusFilter,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<EditorChapterReviewDetail?> GetReviewDetailForEditorAsync(
        Guid chapterId,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<IReadOnlyList<EditorActionableChapterData>> GetActionableChaptersAsync(
        Guid actorUserId,
        Guid? seriesId,
        string? searchText,
        string? statusCode,
        int maxResults,
        CancellationToken ct = default);
}
