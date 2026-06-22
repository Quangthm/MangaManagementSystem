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
    /// EF Core read-only implementation of the Tantou Editor Chapter Review queue and detail.
    /// All queries use <c>AsNoTracking</c>. No writes, no stored procedures. Both the queue and
    /// the detail are scoped to series where the actor is an active Tantou Editor contributor,
    /// so an editor cannot see chapters from series they do not work on.
    /// </summary>
    public class EditorChapterReviewRepository : IEditorChapterReviewRepository
    {
        private const string StatusUnderReview = "UNDER_REVIEW";
        private const string StatusApproved = "APPROVED";
        private const string StatusRevisionRequested = "REVISION_REQUESTED";
        private const string StatusOnHold = "ON_HOLD";
        private const string TantouEditorRole = "Tantou Editor";

        private readonly ApplicationDbContext _dbContext;

        public EditorChapterReviewRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EditorChapterReviewData> GetReviewQueueAsync(
            string? statusFilter, Guid actorUserId, CancellationToken ct = default)
        {
            IQueryable<Guid> scopedSeriesIds = _dbContext.ActiveSeriesContributors
                .AsNoTracking()
                .Where(c => c.UserId == actorUserId && c.RoleName == TantouEditorRole)
                .Select(c => c.SeriesId);

            IQueryable<Chapter> scopedChapters = _dbContext.Chapters
                .AsNoTracking()
                .Where(c => scopedSeriesIds.Contains(c.SeriesId));

            // ── KPI counts (computed in SQL, scoped) ────────────────────────────

            int underReviewCount = await scopedChapters
                .CountAsync(c => c.StatusCode == StatusUnderReview, ct);

            // "Approved This Week" uses CreatedAtUtc as the best-available timestamp because
            // Chapter has no ApprovedAtUtc field.
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            int approvedThisWeekCount = await scopedChapters
                .CountAsync(c => c.StatusCode == StatusApproved && c.CreatedAtUtc >= oneWeekAgo, ct);

            int revisionRequestedCount = await scopedChapters
                .CountAsync(c => c.StatusCode == StatusRevisionRequested, ct);

            int onHoldCount = await scopedChapters
                .CountAsync(c => c.StatusCode == StatusOnHold, ct);

            // ── Chapter list filtered by selected status ────────────────────────

            var reviewableStatuses = new[] { StatusUnderReview, StatusRevisionRequested, StatusOnHold };

            IQueryable<Chapter> baseQuery = _dbContext.Chapters
                .AsNoTracking()
                .Include(c => c.Series)
                .Where(c => scopedSeriesIds.Contains(c.SeriesId));

            if (!string.IsNullOrWhiteSpace(statusFilter) &&
                !string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = baseQuery.Where(c => c.StatusCode == statusFilter);
            }
            else
            {
                baseQuery = baseQuery.Where(c => reviewableStatuses.Contains(c.StatusCode));
            }

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

        public async Task<EditorChapterReviewDetail?> GetReviewDetailForEditorAsync(
            Guid chapterId, Guid actorUserId, CancellationToken ct = default)
        {
            // Access scope: the chapter's series must be one where the actor is an active
            // Tantou Editor contributor. If not, return null so the API responds 403 without
            // leaking any chapter/series data.
            var chapter = await _dbContext.Chapters
                .AsNoTracking()
                .Include(c => c.Series)
                .Include(c => c.CreatedByUser)
                .Where(c => c.ChapterId == chapterId)
                .Where(c => _dbContext.ActiveSeriesContributors
                    .AsNoTracking()
                    .Where(ac => ac.UserId == actorUserId && ac.RoleName == TantouEditorRole)
                    .Select(ac => ac.SeriesId)
                    .Contains(c.SeriesId))
                .FirstOrDefaultAsync(ct);

            if (chapter is null)
            {
                return null;
            }

            // Pages (exclude soft-deleted) with their current version + file URL.
            var pages = await _dbContext.ChapterPages
                .AsNoTracking()
                .Where(cp => cp.ChapterId == chapterId && cp.DeletedAtUtc == null)
                .OrderBy(cp => cp.PageNo)
                .Select(cp => new
                {
                    cp.ChapterPageId,
                    cp.PageNo,
                    CurrentVersion = _dbContext.ChapterPageVersions
                        .Where(v => v.ChapterPageId == cp.ChapterPageId && v.IsCurrentVersion)
                        .Select(v => new
                        {
                            v.ChapterPageVersionId,
                            v.VersionNo,
                            FileUrl = v.PageFile != null ? v.PageFile.CloudinarySecureUrl : null
                        })
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            var pageDetails = pages
                .Select(p => new EditorChapterReviewDetailPage(
                    p.ChapterPageId,
                    p.PageNo,
                    p.CurrentVersion != null ? p.CurrentVersion.ChapterPageVersionId : (Guid?)null,
                    p.CurrentVersion?.FileUrl,
                    p.CurrentVersion != null ? (short?)p.CurrentVersion.VersionNo : null))
                .ToList();

            // Open (unresolved) annotations attached to this chapter via page regions:
            // ChapterPageAnnotation -> PageRegions -> ChapterPageVersion -> ChapterPage.ChapterId
            var openAnnotations = await _dbContext.ChapterPageAnnotations
                .AsNoTracking()
                .Where(a => a.ResolvedAtUtc == null)
                .Where(a => a.PageRegions.Any(r =>
                    r.ChapterPageVersion != null &&
                    r.ChapterPageVersion.ChapterPage != null &&
                    r.ChapterPageVersion.ChapterPage.ChapterId == chapterId))
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => new EditorChapterReviewDetailAnnotation(
                    a.ChapterPageAnnotationId,
                    a.AnnotationText ?? string.Empty,
                    a.IssueTypeCode,
                    a.CreatedAtUtc,
                    a.AnnotatedByUser != null ? a.AnnotatedByUser.DisplayName : null,
                    a.ResolvedAtUtc != null))
                .ToListAsync(ct);

            int pageCount = pageDetails.Count;

            return new EditorChapterReviewDetail(
                chapter.ChapterId,
                chapter.SeriesId,
                chapter.Series != null ? chapter.Series.Title : string.Empty,
                chapter.Series?.Slug,
                chapter.ChapterNumberLabel,
                chapter.ChapterTitle,
                chapter.StatusCode,
                pageCount,
                chapter.CreatedAtUtc,
                chapter.CreatedByUser?.DisplayName,
                pageDetails,
                openAnnotations);
        }


    }
}
