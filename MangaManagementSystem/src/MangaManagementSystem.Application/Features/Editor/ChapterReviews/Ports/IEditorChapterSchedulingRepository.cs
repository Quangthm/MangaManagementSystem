using MangaManagementSystem.Application.DTOs.Publication;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Ports;

public interface IEditorChapterSchedulingRepository
{
    Task<ChapterPlannedDateResult> SetPlannedReleaseDateAsync(
        Guid actorUserId,
        Guid chapterId,
        DateTime plannedReleaseDate,
        CancellationToken ct = default);
}
