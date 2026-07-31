using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Ports;

public interface IEditorChapterReviewWriteRepository
{
    Task<ChapterEditorialReviewResult> SubmitChapterEditorialReviewAsync(
        Guid actorUserId,
        Guid chapterId,
        string decisionCode,
        string? comments,
        UploadedFileMetadata? markup,
        CancellationToken ct = default);

    Task<ChapterEditorialReviewResult> SubmitChapterEditorialReviewWithSchedulingAsync(
        Guid actorUserId,
        Guid chapterId,
        string decisionCode,
        string? comments,
        UploadedFileMetadata? markup,
        CancellationToken ct = default);
}
