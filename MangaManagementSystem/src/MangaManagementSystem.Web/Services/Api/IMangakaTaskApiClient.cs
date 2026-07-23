using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IMangakaTaskApiClient
    {
        Task<IReadOnlyList<ChapterPageTaskDto>> GetTasksForReviewAsync(CancellationToken cancellationToken = default);
        Task<ChapterPageTaskDto?> GetTaskDetailAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task ApproveTaskAsync(Guid taskId, string? reason = null, CancellationToken cancellationToken = default);
        Task ReturnTaskForReworkAsync(Guid taskId, string reason, CancellationToken cancellationToken = default);
        Task CancelTaskAsync(Guid taskId, string reason, CancellationToken cancellationToken = default);
        Task<ChapterPageTaskDto> CreateTaskAsync(CreateMangakaTaskRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChapterPageTaskDto>> GetTasksByPageAsync(Guid chapterPageId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EligibleAssistantDto>> GetEligibleAssistantsAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task<ReassignChapterPageTaskResult> ReassignTaskAsync(Guid taskId, ReassignChapterPageTaskRequest request, CancellationToken cancellationToken = default);

        // Quick Select
        Task<IReadOnlyList<QuickSelectChapterDto>> GetQuickSelectChaptersAsync(Guid seriesId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<QuickSelectPageDto>> GetQuickSelectPagesAsync(Guid chapterId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<QuickSelectAssistantDto>> GetQuickSelectAssistantsAsync(Guid seriesId, CancellationToken cancellationToken = default);
        Task<QuickSelectTaskAssignmentResult> QuickSelectAssignAsync(QuickSelectTaskAssignmentRequest request, CancellationToken cancellationToken = default);
    }
}
