using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
    public partial interface IEditorChapterReviewRepository
    {
        Task<ChapterEditorialReviewResult> SubmitChapterEditorialReviewWithSchedulingAsync(
            Guid actorUserId,
            Guid chapterId,
            string decisionCode,
            string? comments,
            UploadedFileMetadata? markup,
            CancellationToken ct = default);

        Task<ChapterPlannedDateResult> SetPlannedReleaseDateAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime plannedReleaseDate,
            CancellationToken ct = default);
    }
}
