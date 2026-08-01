using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IChapterPageRepository
    {
        Task<ChapterPage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChapterPage>> GetByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, int>> GetPageCountsByChapterIdsAsync(IEnumerable<Guid> chapterIds, CancellationToken cancellationToken = default);
        Task AddAsync(ChapterPage page, CancellationToken cancellationToken = default);
        void Update(ChapterPage page);
    }
}
