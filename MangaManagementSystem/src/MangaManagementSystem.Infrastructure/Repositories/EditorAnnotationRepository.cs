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
    public class EditorAnnotationRepository : IEditorAnnotationRepository
    {
        private const string TantouEditorRole = "Tantou Editor";

        private readonly ApplicationDbContext _dbContext;

        public EditorAnnotationRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EditorAnnotationData> GetAnnotationsAsync(
            Guid actorUserId,
            Guid? seriesId,
            string? issueType,
            string? status,
            CancellationToken ct = default)
        {
            IQueryable<Guid> scopedSeriesIds = _dbContext.ActiveSeriesContributors
                .AsNoTracking()
                .Where(c => c.UserId == actorUserId && c.RoleName == TantouEditorRole)
                .Select(c => c.SeriesId);

            IQueryable<ChapterPageAnnotation> baseQuery = _dbContext.ChapterPageAnnotations
                .AsNoTracking()
                .Include(a => a.PageRegions)
                .Include(a => a.AnnotatedByUser)
                .Where(a => a.AnnotatedByUserId == actorUserId)
                .Where(a => a.PageRegions.Any(r =>
                    r.ChapterPageVersion != null &&
                    r.ChapterPageVersion.ChapterPage != null &&
                    r.ChapterPageVersion.ChapterPage.Chapter != null &&
                    scopedSeriesIds.Contains(r.ChapterPageVersion.ChapterPage.Chapter.SeriesId)));

            if (!string.IsNullOrWhiteSpace(issueType) &&
                !string.Equals(issueType, "all", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = baseQuery.Where(a => a.IssueTypeCode == issueType);
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(status, "open", StringComparison.OrdinalIgnoreCase))
                {
                    baseQuery = baseQuery.Where(a => a.ResolvedAtUtc == null);
                }
                else if (string.Equals(status, "resolved", StringComparison.OrdinalIgnoreCase))
                {
                    baseQuery = baseQuery.Where(a => a.ResolvedAtUtc != null);
                }
            }

            var annotations = await baseQuery
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => new
                {
                    a.ChapterPageAnnotationId,
                    a.IssueTypeCode,
                    a.AnnotationText,
                    a.ResolvedAtUtc,
                    a.CreatedAtUtc,
                    Regions = a.PageRegions.Select(r => new
                    {
                        r.PageRegionId,
                        r.TypeCode,
                        r.X,
                        r.Y,
                        r.Width,
                        r.Height,
                        ChapterPageVersionId = r.ChapterPageVersionId,
                        ChapterPageId = r.ChapterPageVersion != null ? r.ChapterPageVersion.ChapterPageId : (Guid?)null,
                        PageNo = r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPage != null
                            ? (int?)r.ChapterPageVersion.ChapterPage.PageNo
                            : null,
                        ChapterId = r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPage != null && r.ChapterPageVersion.ChapterPage.Chapter != null
                            ? (Guid?)r.ChapterPageVersion.ChapterPage.Chapter.ChapterId
                            : null,
                        ChapterNumberLabel = r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPage != null && r.ChapterPageVersion.ChapterPage.Chapter != null
                            ? r.ChapterPageVersion.ChapterPage.Chapter.ChapterNumberLabel
                            : null,
                        ChapterTitle = r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPage != null && r.ChapterPageVersion.ChapterPage.Chapter != null
                            ? r.ChapterPageVersion.ChapterPage.Chapter.ChapterTitle
                            : null,
                        VersionNo = r.ChapterPageVersion != null ? (short?)r.ChapterPageVersion.VersionNo : null,
                        SeriesId = r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPage != null && r.ChapterPageVersion.ChapterPage.Chapter != null
                            ? (Guid?)r.ChapterPageVersion.ChapterPage.Chapter.SeriesId
                            : null,
                        SeriesTitle = r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPage != null && r.ChapterPageVersion.ChapterPage.Chapter != null && r.ChapterPageVersion.ChapterPage.Chapter.Series != null
                            ? r.ChapterPageVersion.ChapterPage.Chapter.Series.Title
                            : null,
                        SeriesSlug = r.ChapterPageVersion != null && r.ChapterPageVersion.ChapterPage != null && r.ChapterPageVersion.ChapterPage.Chapter != null && r.ChapterPageVersion.ChapterPage.Chapter.Series != null
                            ? r.ChapterPageVersion.ChapterPage.Chapter.Series.Slug
                            : null
                    })
                })
                .ToListAsync(ct);

            var seriesGroups = annotations
                .SelectMany(a => a.Regions, (a, r) => new
                {
                    a.ChapterPageAnnotationId,
                    a.IssueTypeCode,
                    a.AnnotationText,
                    a.ResolvedAtUtc,
                    a.CreatedAtUtc,
                    r.PageRegionId,
                    r.TypeCode,
                    r.X,
                    r.Y,
                    r.Width,
                    r.Height,
                    r.ChapterPageVersionId,
                    r.ChapterPageId,
                    r.PageNo,
                    r.ChapterId,
                    r.ChapterNumberLabel,
                    r.ChapterTitle,
                    r.VersionNo,
                    r.SeriesId,
                    r.SeriesTitle,
                    r.SeriesSlug
                })
                .GroupBy(x => new { x.SeriesId, x.SeriesTitle, x.SeriesSlug })
                .Select(sg =>
                {
                    var annotationGroups = sg
                        .GroupBy(x => x.ChapterPageAnnotationId)
                        .Select(ag =>
                        {
                            var first = ag.First();
                            var regions = ag.Select(r => new EditorAnnotationRegionItem(
                                r.PageRegionId,
                                r.TypeCode,
                                r.X,
                                r.Y,
                                r.Width,
                                r.Height)).ToList();

                            return new EditorAnnotationRow(
                                first.ChapterPageAnnotationId,
                                first.ChapterId ?? Guid.Empty,
                                first.ChapterNumberLabel ?? string.Empty,
                                first.ChapterTitle,
                                first.ChapterPageId ?? Guid.Empty,
                                first.PageNo ?? 0,
                                first.ChapterPageVersionId,
                                first.VersionNo,
                                first.IssueTypeCode,
                                first.AnnotationText,
                                first.ResolvedAtUtc != null,
                                first.CreatedAtUtc,
                                first.ResolvedAtUtc,
                                regions);
                        })
                        .ToList();

                    return new EditorAnnotationSeriesGroup(
                        sg.Key.SeriesId ?? Guid.Empty,
                        sg.Key.SeriesTitle ?? string.Empty,
                        sg.Key.SeriesSlug,
                        annotationGroups);
                })
                .ToList();

            if (seriesId.HasValue && seriesId.Value != Guid.Empty)
            {
                seriesGroups = seriesGroups
                    .Where(g => g.SeriesId == seriesId.Value)
                    .ToList();
            }

            int totalOpen = seriesGroups.Sum(g => g.Annotations.Count(a => !a.IsResolved));
            int totalResolved = seriesGroups.Sum(g => g.Annotations.Count(a => a.IsResolved));
            int pagesWithIssues = seriesGroups
                .SelectMany(g => g.Annotations)
                .Select(a => a.ChapterPageId)
                .Distinct()
                .Count();
            int distinctIssueTypes = seriesGroups
                .SelectMany(g => g.Annotations)
                .Select(a => a.IssueTypeCode)
                .Distinct()
                .Count();

            var seriesFilters = seriesGroups
                .Select(g => new EditorAnnotationSeriesFilterItem(g.SeriesId, g.SeriesTitle))
                .ToList();

            return new EditorAnnotationData(
                totalOpen,
                totalResolved,
                pagesWithIssues,
                distinctIssueTypes,
                seriesFilters,
                Array.Empty<string>(),
                seriesGroups);
        }


    }
}
