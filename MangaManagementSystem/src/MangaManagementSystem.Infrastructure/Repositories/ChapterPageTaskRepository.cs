using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterPageTaskRepository : GenericRepository<ChapterPageTask>, IChapterPageTaskRepository
    {
        private readonly ApplicationDbContext _context;
        public ChapterPageTaskRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Guid> CreateChapterPageTaskAsync(
            Guid actorUserId,
            Guid assignedToUserId,
            string typeCode,
            string taskTitle,
            string taskDescription,
            byte priorityLevel,
            DateTime dueAtUtc,
            decimal? compensationAmount,
            IReadOnlyList<Guid> pageRegionIds)
        {
            var task = new ChapterPageTask
            {
                ChapterPageTaskId = Guid.NewGuid(),
                AssignedToUserId = assignedToUserId,
                TypeCode = typeCode,
                TaskTitle = taskTitle,
                TaskDescription = taskDescription,
                PriorityLevel = priorityLevel,
                DueAtUtc = dueAtUtc,
                CompensationAmount = compensationAmount ?? 0m,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = DateTime.UtcNow,
                StatusCode = "ASSIGNED"
            };

            if (pageRegionIds != null && pageRegionIds.Any())
            {
                var regions = await _context.PageRegions
                    .Where(r => pageRegionIds.Contains(r.PageRegionId))
                    .ToListAsync();
                    
                foreach (var region in regions)
                {
                    task.PageRegions.Add(region);
                }
            }

            await _context.ChapterPageTasks.AddAsync(task);
            await _context.SaveChangesAsync();

            return task.ChapterPageTaskId;
        }

        public async Task<ChapterPageTask?> GetByIdWithRegionsAsync(Guid id)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.PageRegions)
                .FirstOrDefaultAsync(t => t.ChapterPageTaskId == id);
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetByAssignedUserIdWithRegionsAsync(Guid assignedToUserId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.PageRegions)
                .Where(t => t.AssignedToUserId == assignedToUserId)
                .OrderBy(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetByCreatorUserIdWithSeriesAsync(Guid creatorUserId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.ChapterPage)
                            .ThenInclude(p => p.Chapter)
                .Include(t => t.AssignedToUser)
                .Where(t => t.CreatedByUserId == creatorUserId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetByAssignedUserIdWithFullContextAsync(Guid assignedToUserId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.ChapterPage)
                            .ThenInclude(p => p.Chapter)
                                .ThenInclude(c => c.Series)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.PageFile)
                .Where(t => t.AssignedToUserId == assignedToUserId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<ChapterPageTask?> GetByIdWithFullContextAsync(Guid taskId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.ChapterPage)
                            .ThenInclude(p => p.Chapter)
                                .ThenInclude(c => c.Series)
                .Include(t => t.PageRegions)
                    .ThenInclude(r => r.ChapterPageVersion)
                        .ThenInclude(v => v.PageFile)
                .FirstOrDefaultAsync(t => t.ChapterPageTaskId == taskId);
        }

        public async Task<IReadOnlyList<ChapterPageTask>> GetByChapterPageIdWithRegionsAsync(Guid chapterPageId)
        {
            return await _context.ChapterPageTasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.PageRegions)
                .Where(t => t.PageRegions.Any(r => r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPageId == chapterPageId))
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }
    }
}
