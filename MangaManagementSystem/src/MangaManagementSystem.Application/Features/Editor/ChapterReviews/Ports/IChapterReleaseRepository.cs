using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Models;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Ports;

public interface IChapterReleaseRepository
{
    Task<ChapterReleaseResult> ReleaseChapterAsync(
        Guid actorUserId,
        Guid chapterId,
        bool confirmRelease,
        CancellationToken ct = default);
}
