using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterPageVersionRepository : IChapterPageVersionRepository
    {
        private readonly ApplicationDbContext _context;

        public ChapterPageVersionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChapterPageVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ChapterPageVersions
                .FirstOrDefaultAsync(v => v.ChapterPageVersionId == id, cancellationToken);
        }

        public async Task<IReadOnlyList<ChapterPageVersion>> GetByPageIdAsync(Guid chapterPageId, CancellationToken cancellationToken = default)
        {
            return await _context.ChapterPageVersions
                .Where(v => v.ChapterPageId == chapterPageId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ChapterPageVersion>> GetByPageIdsAsync(IEnumerable<Guid> chapterPageIds, CancellationToken cancellationToken = default)
        {
            var idList = chapterPageIds.ToList();
            return await _context.ChapterPageVersions
                .Where(v => idList.Contains(v.ChapterPageId))
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(ChapterPageVersion version, CancellationToken cancellationToken = default)
        {
            await _context.ChapterPageVersions.AddAsync(version, cancellationToken);
        }

        public void Update(ChapterPageVersion version)
        {
            _context.ChapterPageVersions.Update(version);
        }
    }
}
