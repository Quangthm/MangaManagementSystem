using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IChapterRepository : IGenericRepository<Chapter>
    {
        Task DeleteWithDependenciesAsync(Guid chapterId);
    }
}
