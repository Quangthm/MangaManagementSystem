using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public sealed record ChapterOnHoldResult(
        Guid ChapterId,
        string StatusCode,
        string Message);

    public interface IChapterOnHoldRepository
    {
        Task<ChapterOnHoldResult> PutScheduledChapterOnHoldAsync(
            Guid actorUserId,
            Guid chapterId,
            string reason,
            CancellationToken ct = default);
    }
}
