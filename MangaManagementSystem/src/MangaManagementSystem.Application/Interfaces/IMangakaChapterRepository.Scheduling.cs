using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Services;

namespace MangaManagementSystem.Application.Interfaces
{
    public partial interface IMangakaChapterRepository
    {
        Task<SetChapterPlannedReleaseDateResponse> SetPlannedReleaseDateAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime plannedReleaseDate,
            ChapterSchedulingValidator schedulingValidator,
            CancellationToken cancellationToken = default);
    }
}
