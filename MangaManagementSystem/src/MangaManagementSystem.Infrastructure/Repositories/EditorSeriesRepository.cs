using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class EditorSeriesRepository : IEditorSeriesRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public EditorSeriesRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Series>> GetSeriesAsync(
            Guid actorUserId, CancellationToken ct = default)
        {
            return await _dbContext.Series
                .AsNoTracking()
                .Where(s => _dbContext.ActiveSeriesContributors
                    .Any(asc => asc.SeriesId == s.SeriesId && asc.UserId == actorUserId))
                .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
                .ToListAsync(ct);
        }
    }
}
