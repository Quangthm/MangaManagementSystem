using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class LandingPageRepository : ILandingPageRepository
    {
        private const string ChapterStatusUnderReview = "UNDER_REVIEW";
        private const string TaskStatusAssigned = "ASSIGNED";
        private const string TaskStatusCompleted = "COMPLETED";
        private const string PollStatusOpen = "OPEN";

        private readonly ApplicationDbContext _dbContext;

        public LandingPageRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<LandingPageData> GetDashboardDataAsync(CancellationToken ct = default)
        {
            int activeReviews = await _dbContext.Chapters
                .AsNoTracking()
                .CountAsync(c => c.StatusCode == ChapterStatusUnderReview, ct);

            int pendingTasks = await _dbContext.ChapterPageTasks
                .AsNoTracking()
                .CountAsync(t => t.StatusCode == TaskStatusAssigned, ct);

            int openBoardPolls = await _dbContext.SeriesBoardPolls
                .AsNoTracking()
                .CountAsync(p => p.PollStatusCode == PollStatusOpen, ct);

            int completedTasks = await _dbContext.ChapterPageTasks
                .AsNoTracking()
                .CountAsync(t => t.StatusCode == TaskStatusCompleted, ct);

            int totalSeries = await _dbContext.Series
                .AsNoTracking()
                .CountAsync(ct);

            int totalChapters = await _dbContext.Chapters
                .AsNoTracking()
                .CountAsync(ct);

            int releasedChapters = await _dbContext.Chapters
                .AsNoTracking()
                .CountAsync(c => c.ReleasedAtUtc != null, ct);

            var items = new List<LandingPageActivityItem>();

            List<Domain.Entities.ChapterPageTask> recentTasks = await _dbContext.ChapterPageTasks
                .AsNoTracking()
                .Where(t => t.StatusCode == TaskStatusCompleted)
                .OrderByDescending(t => t.UpdatedAtUtc ?? t.CreatedAtUtc)
                .Take(3)
                .ToListAsync(ct);

            foreach (var t in recentTasks)
            {
                items.Add(new LandingPageActivityItem(
                    t.TaskTitle,
                    "submitted",
                    t.UpdatedAtUtc ?? t.CreatedAtUtc));
            }

            List<Domain.Entities.Chapter> recentChapters = await _dbContext.Chapters
                .AsNoTracking()
                .Include(c => c.Series)
                .Where(c => c.ReleasedAtUtc != null)
                .OrderByDescending(c => c.ReleasedAtUtc)
                .Take(3)
                .ToListAsync(ct);

            foreach (var c in recentChapters)
            {
                string seriesTitle = c.Series?.Title ?? "";
                items.Add(new LandingPageActivityItem(
                    $"{c.ChapterNumberLabel} — {seriesTitle}",
                    "approved",
                    c.ReleasedAtUtc!.Value));
            }

            List<Domain.Entities.SeriesBoardPoll> recentPolls = await _dbContext.SeriesBoardPolls
                .AsNoTracking()
                .Include(p => p.Series)
                .OrderByDescending(p => p.StartedAtUtc)
                .Take(2)
                .ToListAsync(ct);

            foreach (var p in recentPolls)
            {
                items.Add(new LandingPageActivityItem(
                    p.Series?.Title ?? "Unknown series",
                    "opened",
                    p.StartedAtUtc));
            }

            var activities = items
                .OrderByDescending(i => i.OccurredAtUtc)
                .Take(5)
                .ToList();

            return new LandingPageData(
                activeReviews,
                pendingTasks,
                openBoardPolls,
                completedTasks,
                totalSeries,
                totalChapters,
                releasedChapters,
                activities);
        }
    }
}
