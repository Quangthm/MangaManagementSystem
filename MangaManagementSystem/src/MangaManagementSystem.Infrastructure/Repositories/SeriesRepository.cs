using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class SeriesRepository : GenericRepository<Series>, ISeriesRepository
    {
        public SeriesRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Series?> GetSeriesWithChaptersAsync(Guid seriesId)
        {
            return await _context.Series
                .Include(s => s.Chapters)
                .FirstOrDefaultAsync(s => s.SeriesId == seriesId);
        }
    }
}
