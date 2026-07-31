using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Ports
{
    public partial interface IMangakaChapterRepository
    {
        Task<SetChapterPlannedReleaseDateResponse> SetPlannedReleaseDateAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime plannedReleaseDate,
            CancellationToken cancellationToken = default);
    }
}
