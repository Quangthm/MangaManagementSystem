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
    public partial class EditorChapterReviewRepository
    {
        private static readonly string[] ActionableChapterStatuses =
        {
            StatusDraft, StatusUnderReview, StatusRevisionRequested,
            StatusApproved, StatusScheduled, StatusOnHold
        };

        public async Task<IReadOnlyList<EditorActionableChapterData>> GetActionableChaptersAsync(
            Guid actorUserId,
            Guid? seriesId,
            string? searchText,
            string? statusCode,
            int maxResults,
            CancellationToken ct = default)
        {
            IQueryable<Guid> scopedSeriesIds = _dbContext.ActiveSeriesContributors
                .AsNoTracking()
                .Where(c => c.UserId == actorUserId && c.RoleName == TantouEditorRole)
                .Select(c => c.SeriesId);

            IQueryable<Chapter> query = _dbContext.Chapters
                .AsNoTracking()
                .Where(c => scopedSeriesIds.Contains(c.SeriesId));

            query = query.Where(c => !(c.StatusCode == StatusReleased || c.StatusCode == StatusCancelled));

            if (!string.IsNullOrWhiteSpace(statusCode))
            {
                if (ActionableChapterStatuses.Contains(statusCode))
                {
                    query = query.Where(c => c.StatusCode == statusCode);
                }
            }

            if (seriesId.HasValue && seriesId.Value != Guid.Empty)
            {
                query = query.Where(c => c.SeriesId == seriesId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var lower = searchText.ToLowerInvariant();
                query = query.Where(c =>
                    (c.Series != null && c.Series.Title.ToLower().Contains(lower)) ||
                    c.ChapterNumberLabel.ToLower().Contains(lower) ||
                    (c.ChapterTitle != null && c.ChapterTitle.ToLower().Contains(lower)));
            }

            query = query
                .OrderBy(c => c.Series != null ? c.Series.Title : "")
                .ThenBy(c => c.ChapterNumberLabel)
                .ThenBy(c => c.PlannedReleaseDate.HasValue ? 0 : 1)
                .ThenBy(c => c.PlannedReleaseDate);

            var results = await query
                .Take(maxResults)
                .Select(c => new EditorActionableChapterData(
                    c.ChapterId,
                    c.SeriesId,
                    c.Series != null ? c.Series.Title : "",
                    c.Series != null ? c.Series.Slug : null,
                    c.Series != null && c.Series.CoverFile != null
                        ? c.Series.CoverFile.CloudinarySecureUrl
                        : null,
                    c.ChapterNumberLabel,
                    c.ChapterTitle,
                    c.StatusCode,
                    c.PlannedReleaseDate,
                    c.ReleasedAtUtc,
                    c.Series != null ? c.Series.PublicationFrequencyCode : null,
                    c.UpdatedAtUtc))
                .ToListAsync(ct);

            return results.AsReadOnly();
        }
    }
}
