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
    }
}
