using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ReferenceDataRepository : IReferenceDataRepository
    {
        private readonly ApplicationDbContext _context;

        public ReferenceDataRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Genre>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Genres
                .AsNoTracking()
                .OrderBy(g => g.GenreName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Tag>> GetTagsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Tags
                .AsNoTracking()
                .OrderBy(t => t.TagName)
                .ToListAsync(cancellationToken);
        }
    }
}
