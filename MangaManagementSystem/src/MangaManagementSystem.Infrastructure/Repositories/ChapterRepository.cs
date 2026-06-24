using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterRepository : GenericRepository<Chapter>, IChapterRepository
    {
        public ChapterRepository(ApplicationDbContext context) : base(context) { }

        public async Task DeleteWithDependenciesAsync(Guid chapterId)
        {
            await _context.Database.ExecuteSqlRawAsync("EXEC manga.usp_Chapter_Delete {0}", chapterId);
        }
    }
}
