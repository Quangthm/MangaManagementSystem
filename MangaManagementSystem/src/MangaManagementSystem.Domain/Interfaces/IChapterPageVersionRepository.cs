using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IChapterPageVersionRepository
    {
        Task<ChapterPageVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChapterPageVersion>> GetByPageIdAsync(Guid chapterPageId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChapterPageVersion>> GetByPageIdsAsync(IEnumerable<Guid> chapterPageIds, CancellationToken cancellationToken = default);
        Task AddAsync(ChapterPageVersion version, CancellationToken cancellationToken = default);
        void Update(ChapterPageVersion version);
    }
}
