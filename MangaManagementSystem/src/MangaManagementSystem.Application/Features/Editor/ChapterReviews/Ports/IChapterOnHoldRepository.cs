using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Ports;

public interface IChapterOnHoldRepository
{
    Task<ChapterOnHoldResult> PutScheduledChapterOnHoldAsync(
        Guid actorUserId,
        Guid chapterId,
        string reason,
        CancellationToken ct = default);
}
