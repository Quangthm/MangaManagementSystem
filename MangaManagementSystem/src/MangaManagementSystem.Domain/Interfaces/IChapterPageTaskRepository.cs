using MangaManagementSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IChapterPageTaskRepository : IGenericRepository<ChapterPageTask>
    {
        Task<Guid> CreateChapterPageTaskAsync(
            Guid actorUserId,
            Guid assignedToUserId,
            string typeCode,
            string taskTitle,
            string taskDescription,
            byte priorityLevel,
            DateTime dueAtUtc,
            decimal? compensationAmount,
            IReadOnlyList<Guid> pageRegionIds);

        Task<ChapterPageTask?> GetByIdWithRegionsAsync(Guid id);

        Task<IReadOnlyList<ChapterPageTask>> GetByAssignedUserIdWithRegionsAsync(Guid assignedToUserId);

        Task<IReadOnlyList<ChapterPageTask>> GetByCreatorUserIdWithSeriesAsync(Guid creatorUserId);

        Task<IReadOnlyList<ChapterPageTask>> GetByAssignedUserIdWithFullContextAsync(Guid assignedToUserId);

        Task<ChapterPageTask?> GetByIdWithFullContextAsync(Guid taskId);

        Task<IReadOnlyList<ChapterPageTask>> GetByChapterPageIdWithRegionsAsync(Guid chapterPageId);
        // Mangaka task lifecycle SPs
        Task CancelTaskAsync(Guid actorUserId, Guid taskId, string reason);
        Task MarkTaskCompletedAsync(Guid actorUserId, Guid taskId, string? completionNote);
        Task ReturnTaskForReworkAsync(Guid actorUserId, Guid taskId, string updatedTaskDescription);

        // Mangaka: tasks created by this user (for review submissions view)
        Task<IReadOnlyList<ChapterPageTask>> GetTasksForReviewByCreatorAsync(Guid creatorUserId);
    }
}
