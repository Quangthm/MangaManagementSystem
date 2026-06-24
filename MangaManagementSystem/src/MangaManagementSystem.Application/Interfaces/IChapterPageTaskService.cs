using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageTaskService
    {
        Task<ChapterPageTaskDto> CreateChapterPageTaskAsync(CreateChapterPageTaskDto dto);
        Task<ChapterPageTaskDto?> GetChapterPageTaskByIdAsync(Guid id);
        Task<ChapterPageTaskDto?> GetChapterPageTaskByIdWithRegionsAsync(Guid id);
        Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByAssignedUserIdAsync(Guid assignedToUserId);
        Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByAssignedUserIdWithRegionsAsync(Guid assignedToUserId);
        Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByCreatorUserIdAsync(Guid creatorUserId);
        Task<ChapterPageTaskDto?> UpdateChapterPageTaskAsync(UpdateChapterPageTaskDto dto);
        Task<bool> DeleteChapterPageTaskAsync(Guid id);

        // Assistant read operations
        Task<IEnumerable<ChapterPageTaskDto>> GetAssignedTasksForAssistantAsync(Guid assistantUserId);
        Task<ChapterPageTaskDto?> GetAssignedTaskDetailForAssistantAsync(Guid assistantUserId, Guid taskId);
        Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByChapterPageIdAsync(Guid chapterPageId);

        // Mangaka task lifecycle actions
        Task ApproveTaskAsync(Guid actorUserId, Guid taskId, string? completionNote);
        Task ReturnTaskForReworkAsync(Guid actorUserId, Guid taskId, string reason);
        Task CancelTaskAsync(Guid actorUserId, Guid taskId, string reason);

        // Mangaka: tasks created by this user (for review submissions view)
        Task<IEnumerable<ChapterPageTaskDto>> GetTasksForReviewByCreatorAsync(Guid creatorUserId);

        // Mangaka: reassign task to different assistant
        Task<ReassignChapterPageTaskResult> ReassignTaskAsync(
            Guid actorUserId,
            Guid taskId,
            ReassignChapterPageTaskRequest request);

        // Mangaka: eligible assistants for task reassignment
        Task<IReadOnlyList<EligibleAssistantDto>> GetEligibleAssistantsForTaskAsync(
            Guid actorUserId,
            Guid taskId);
    }
}
