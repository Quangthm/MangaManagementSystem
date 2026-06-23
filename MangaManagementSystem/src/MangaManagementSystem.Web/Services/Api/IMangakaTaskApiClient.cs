using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IMangakaTaskApiClient
    {
        Task<IReadOnlyList<ChapterPageTaskDto>> GetTasksForReviewAsync(Guid actorUserId, CancellationToken cancellationToken = default);
        Task<ChapterPageTaskDto?> GetTaskDetailAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken = default);
        Task ApproveTaskAsync(Guid actorUserId, Guid taskId, string? reason = null, CancellationToken cancellationToken = default);
        Task ReturnTaskForReworkAsync(Guid actorUserId, Guid taskId, string reason, CancellationToken cancellationToken = default);
        Task CancelTaskAsync(Guid actorUserId, Guid taskId, string reason, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EligibleAssistantDto>> GetEligibleAssistantsAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken = default);
        Task<ReassignChapterPageTaskResult> ReassignTaskAsync(Guid actorUserId, Guid taskId, ReassignChapterPageTaskRequest request, CancellationToken cancellationToken = default);
    }
}
