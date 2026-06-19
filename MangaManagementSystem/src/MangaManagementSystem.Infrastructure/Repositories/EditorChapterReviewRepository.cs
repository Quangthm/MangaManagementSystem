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
    /// <summary>
    /// EF Core read-only implementation of the Tantou Editor Chapter Review Queue.
    /// All queries use <c>AsNoTracking</c>. No writes, no stored procedures. KPI counts are
    /// computed server-side in SQL; only the filtered chapter list is materialised.
    ///
    /// MVP scoping: the queue is global (no per-Tantou-Editor contributor scope). The current
    /// schema has no clear actor-specific relationship for chapter review assignments.
    /// </summary>
    public class EditorChapterReviewRepository : IEditorChapterReviewRepository
    {
        private const string StatusUnderReview = "UNDER_REVIEW";
        private const string StatusApproved = "APPROVED";
        private const string StatusRevisionRequested = "REVISION_REQUESTED";
        private const string StatusOnHold = "ON_HOLD";

        private readonly ApplicationDbContext _dbContext;

        public EditorChapterReviewRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EditorChapterReviewData> GetReviewQueueAsync(
            string? statusFilter, CancellationToken ct = default)
        {
            // ── KPI counts (computed in SQL) ────────────────────────────────────

            int underReviewCount = await _dbContext.Chapters
                .AsNoTracking()
                .CountAsync(c => c.StatusCode == StatusUnderReview, ct);

            // "Approved This Week" uses CreatedAtUtc as the best-available timestamp because
            // Chapter has no ApprovedAtUtc field.
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            int approvedThisWeekCount = await _dbContext.Chapters
                .AsNoTracking()
                .CountAsync(c => c.StatusCode == StatusApproved && c.CreatedAtUtc >= oneWeekAgo, ct);

            int revisionRequestedCount = await _dbContext.Chapters
                .AsNoTracking()
                .CountAsync(c => c.StatusCode == StatusRevisionRequested, ct);

            int onHoldCount = await _dbContext.Chapters
                .AsNoTracking()
                .CountAsync(c => c.StatusCode == StatusOnHold, ct);

            // ── Chapter list filtered by selected status ────────────────────────

            var reviewableStatuses = new[] { StatusUnderReview, StatusRevisionRequested, StatusOnHold };

            IQueryable<Chapter> baseQuery = _dbContext.Chapters
                .AsNoTracking()
                .Include(c => c.Series);

            if (!string.IsNullOrWhiteSpace(statusFilter) &&
                !string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = baseQuery.Where(c => c.StatusCode == statusFilter);
            }
            else
            {
                baseQuery = baseQuery.Where(c => reviewableStatuses.Contains(c.StatusCode));
            }

            // Project into the Domain record with a correlated page-count subquery so that
            // page counts are computed in SQL and we don't need a Chapter->ChapterPages nav prop.
            List<EditorChapterReviewChapter> chapters = await baseQuery
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new EditorChapterReviewChapter(
                    c.ChapterId,
                    c.SeriesId,
                    c.ChapterNumberLabel,
                    c.ChapterTitle,
                    c.StatusCode,
                    _dbContext.ChapterPages
                        .Count(cp => cp.ChapterId == c.ChapterId && cp.DeletedAtUtc == null),
                    c.CreatedAtUtc,
                    c.Series))
                .ToListAsync(ct);

            return new EditorChapterReviewData(
                underReviewCount,
                approvedThisWeekCount,
                revisionRequestedCount,
                onHoldCount,
                chapters);
        }
    }
}
