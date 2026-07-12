using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.Interfaces;

public partial interface IMangakaChapterRepository
{
<<<<<<< HEAD
    Task<SetChapterPlannedReleaseDateResponse> SetPlannedReleaseDateAsync(
        Guid actorUserId,
        Guid chapterId,
        DateTime plannedReleaseDate,
        ChapterSchedulingValidator schedulingValidator,
        CancellationToken cancellationToken = default);
=======
    public partial interface IMangakaChapterRepository
    {
        Task<SetChapterPlannedReleaseDateResponse> SetPlannedReleaseDateAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime plannedReleaseDate,
            CancellationToken cancellationToken = default);
    }
>>>>>>> main
}
