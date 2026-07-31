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
    public class ChapterPageRepository : IChapterPageRepository
    {
        private readonly ApplicationDbContext _context;

        public ChapterPageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChapterPage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ChapterPages
                .FirstOrDefaultAsync(p => p.ChapterPageId == id, cancellationToken);
        }

        public async Task<IReadOnlyList<ChapterPage>> GetByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            return await _context.ChapterPages
                .Where(p => p.ChapterId == chapterId && p.DeletedAtUtc == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetPageCountsByChapterIdsAsync(IEnumerable<Guid> chapterIds, CancellationToken cancellationToken = default)
        {
            var idList = chapterIds.ToList();
            return await _context.ChapterPages
                .Where(p => idList.Contains(p.ChapterId) && p.DeletedAtUtc == null)
                .GroupBy(p => p.ChapterId)
                .Select(g => new { ChapterId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.ChapterId, v => v.Count, cancellationToken);
        }

        public async Task AddAsync(ChapterPage page, CancellationToken cancellationToken = default)
        {
            await _context.ChapterPages.AddAsync(page, cancellationToken);
        }

        public void Update(ChapterPage page)
        {
            _context.ChapterPages.Update(page);
        }
    }
}
