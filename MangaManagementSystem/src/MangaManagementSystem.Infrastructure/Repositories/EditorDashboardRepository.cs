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
    /// EF Core read-only implementation of the Tantou Editor dashboard repository. Every query
    /// uses <c>AsNoTracking</c>. No writes, no stored procedures. Counts are computed server-side
    /// in SQL; only the small preview lists are materialised.
    /// </summary>
    public class EditorDashboardRepository : IEditorDashboardRepository
    {
        private const string ProposalStatusUnderEditorialReview = "UNDER_EDITORIAL_REVIEW";
        private const string ChapterStatusUnderReview = "UNDER_REVIEW";
        private const string SeriesStatusSerialized = "SERIALIZED";

        private readonly ApplicationDbContext _dbContext;

        public EditorDashboardRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EditorDashboardData> GetDashboardDataAsync(
            Guid actorUserId, int proposalQueueTake, int recentSeriesTake, CancellationToken ct = default)
        {
            // Active series IDs for the current Tantou Editor.
            var activeSeriesIds = await _dbContext.ActiveSeriesContributors
                .AsNoTracking()
                .Where(asc => asc.UserId == actorUserId)
                .Select(asc => asc.SeriesId)
                .ToListAsync(ct);

            // Pending Proposals: global claimable queue (all Tantou Editors discover unclaimed
            // proposals from here). Not scoped to active contributor series.
            int pendingProposalCount = await _dbContext.SeriesProposals
                .AsNoTracking()
                .CountAsync(sp => sp.StatusCode == ProposalStatusUnderEditorialReview, ct);

            // Chapters Under Review: scoped to series the editor actively contributes to.
            int chaptersUnderReviewCount;
            if (activeSeriesIds.Count > 0)
            {
                chaptersUnderReviewCount = await _dbContext.Chapters
                    .AsNoTracking()
                    .CountAsync(c => c.StatusCode == ChapterStatusUnderReview
                        && activeSeriesIds.Contains(c.SeriesId), ct);
            }
            else
            {
                chaptersUnderReviewCount = 0;
            }

            // Pending Annotations: unresolved annotations only for chapters/pages belonging to
            // the editor's active contributor series.
            int pendingAnnotationCount;
            if (activeSeriesIds.Count > 0)
            {
                pendingAnnotationCount = await _dbContext.ChapterPageAnnotations
                    .AsNoTracking()
                    .Where(a => a.ResolvedAtUtc == null)
                    .Where(a => a.PageRegions.Any(pr =>
                        pr.ChapterPageVersion != null &&
                        pr.ChapterPageVersion.ChapterPage != null &&
                        pr.ChapterPageVersion.ChapterPage.Chapter != null &&
                        activeSeriesIds.Contains(pr.ChapterPageVersion.ChapterPage.Chapter.SeriesId)))
                    .CountAsync(ct);
            }
            else
            {
                pendingAnnotationCount = 0;
            }

            // Serialized Series: already scoped to ActiveSeriesContributors.
            int serializedSeriesCount = await _dbContext.Series
                .AsNoTracking()
                .Where(s => s.StatusCode == SeriesStatusSerialized)
                .Where(s => _dbContext.ActiveSeriesContributors
                    .Any(asc => asc.SeriesId == s.SeriesId && asc.UserId == actorUserId))
                .CountAsync(ct);

            // Proposal review queue preview: newest UNDER_EDITORIAL_REVIEW proposals first.
            List<SeriesProposal> proposalQueue = await _dbContext.SeriesProposals
                .AsNoTracking()
                .Include(sp => sp.Series)
                .Include(sp => sp.SubmittedByUser)
                .Where(sp => sp.StatusCode == ProposalStatusUnderEditorialReview)
                .OrderByDescending(sp => sp.SubmittedAtUtc)
                .Take(proposalQueueTake)
                .ToListAsync(ct);

            // Recent series activity: most recently updated/created series first, scoped to
            // series the current editor contributes to, with chapters eagerly loaded so the
            // handler can derive the latest chapter label.
            List<Series> recentSeries = await _dbContext.Series
                .AsNoTracking()
                .Include(s => s.Chapters)
                .Where(s => _dbContext.ActiveSeriesContributors
                    .Any(asc => asc.SeriesId == s.SeriesId && asc.UserId == actorUserId))
                .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
                .Take(recentSeriesTake)
                .ToListAsync(ct);

            return new EditorDashboardData(
                pendingProposalCount,
                chaptersUnderReviewCount,
                pendingAnnotationCount,
                serializedSeriesCount,
                proposalQueue,
                recentSeries);
        }
    }
}
