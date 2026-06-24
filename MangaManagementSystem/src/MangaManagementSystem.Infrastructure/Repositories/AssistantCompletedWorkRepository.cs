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
    public class AssistantCompletedWorkRepository : IAssistantCompletedWorkRepository
    {
        private readonly ApplicationDbContext _context;

        public AssistantCompletedWorkRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<AssistantCompletedWorkReadModel> GetCompletedWorkAsync(
            Guid assistantUserId, CancellationToken cancellationToken = default)
        {
            var raw = await _context.ChapterPageTasks
                .AsNoTracking()
                .Where(t => t.AssignedToUserId == assistantUserId
                         && t.StatusCode == "COMPLETED")
                .Select(t => new
                {
                    t.ChapterPageTaskId,
                    t.TypeCode,
                    t.StatusCode,
                    t.DueAtUtc,
                    t.CompensationAmount,
                    t.CreatedAtUtc,
                    t.UpdatedAtUtc,
                    Regions = t.PageRegions.Select(r => new
                    {
                        r.PageRegionId,
                        SeriesTitle = r.ChapterPageVersion!.ChapterPage!.Chapter!.Series!.Title,
                        ChapterTitle = r.ChapterPageVersion.ChapterPage.Chapter.ChapterTitle,
                        PageNumber = r.ChapterPageVersion.ChapterPage.PageNo
                    }).ToList()
                })
                .OrderByDescending(t => t.UpdatedAtUtc ?? t.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var tasks = raw.Select(x =>
            {
                var firstRegion = x.Regions.FirstOrDefault();
                return new AssistantCompletedTaskRow
                {
                    ChapterPageTaskId = x.ChapterPageTaskId,
                    TypeCode = x.TypeCode ?? string.Empty,
                    StatusCode = x.StatusCode ?? string.Empty,
                    DueAtUtc = x.DueAtUtc,
                    CompensationAmount = x.CompensationAmount,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    RegionCount = x.Regions.Count,
                    SeriesTitle = firstRegion?.SeriesTitle ?? string.Empty,
                    ChapterTitle = firstRegion?.ChapterTitle ?? string.Empty,
                    PageNumber = firstRegion?.PageNumber ?? 0
                };
            }).ToList();

            return new AssistantCompletedWorkReadModel(tasks);
        }
    }
}