using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class SeriesRepository : GenericRepository<Series>, ISeriesRepository
    {
        private readonly ApplicationDbContext _context;
        public SeriesRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Series?> GetSeriesWithChaptersAsync(long seriesId)
        {
            return await _context.Series
                .Include(s => s.Chapters)
                .FirstOrDefaultAsync(s => s.SeriesId == seriesId);
        }
    }
}
