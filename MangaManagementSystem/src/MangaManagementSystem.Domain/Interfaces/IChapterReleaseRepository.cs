using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public sealed record ChapterReleaseResult(
        Guid ChapterId,
        string StatusCode,
        DateTime ReleasedAtUtc,
        DateTime? PlannedReleaseDate,
        string Message);

    public interface IChapterReleaseRepository
    {
        Task<ChapterReleaseResult> ReleaseChapterAsync(
            Guid actorUserId,
            Guid chapterId,
            bool confirmRelease,
            CancellationToken ct = default);
    }
}
