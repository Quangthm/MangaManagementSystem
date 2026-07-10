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
    public class PublicationScheduleRepository : IPublicationScheduleRepository
    {
        private const string SeriesStatusSerialized = "SERIALIZED";
        private const string ChapterStatusCancelled = "CANCELLED";

        private readonly ApplicationDbContext _dbContext;

        public PublicationScheduleRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<PublicationScheduleChapter>> GetScheduleChaptersAsync(
            DateTime weekStart,
            DateTime weekEnd,
            Guid? seriesId,
            string? frequencyCode,
            CancellationToken ct = default)
        {
            var query = _dbContext.Chapters
                .AsNoTracking()
                .Where(c => c.Series != null && c.Series.StatusCode == SeriesStatusSerialized)
                .Where(c => c.StatusCode != ChapterStatusCancelled)
                .Where(c =>
                    (c.PlannedReleaseDate != null && c.PlannedReleaseDate.Value >= weekStart && c.PlannedReleaseDate.Value < weekEnd.AddDays(1)) ||
                    (c.ReleasedAtUtc != null && c.ReleasedAtUtc.Value >= weekStart && c.ReleasedAtUtc.Value < weekEnd.AddDays(1)));

            if (seriesId.HasValue && seriesId.Value != Guid.Empty)
            {
                query = query.Where(c => c.SeriesId == seriesId.Value);
            }

            if (!string.IsNullOrWhiteSpace(frequencyCode))
            {
                query = query.Where(c =>
                    c.Series != null && c.Series.PublicationFrequencyCode == frequencyCode);
            }

            var results = await query
                .OrderBy(c => c.Series!.Title)
                .ThenBy(c => c.ChapterNumberLabel)
                .Select(c => new PublicationScheduleChapter(
                    c.SeriesId,
                    c.Series != null ? c.Series.Title : string.Empty,
                    c.Series != null ? c.Series.Slug : null,
                    c.Series != null && c.Series.CoverFile != null
                        ? c.Series.CoverFile.CloudinarySecureUrl
                        : null,
                    c.ChapterId,
                    c.ChapterNumberLabel,
                    c.StatusCode,
                    c.PlannedReleaseDate,
                    c.ReleasedAtUtc,
                    c.Series != null ? c.Series.PublicationFrequencyCode : null))
                .ToListAsync(ct);

            return results.AsReadOnly();
        }

        public async Task<IReadOnlyList<PublicationScheduleSeriesSuggestion>> GetSeriesSuggestionsAsync(
            string searchText,
            int maxResults = 10,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return Array.Empty<PublicationScheduleSeriesSuggestion>();
            }

            var results = await _dbContext.Series
                .AsNoTracking()
                .Where(s => s.StatusCode == SeriesStatusSerialized)
                .Where(s => s.Title.Contains(searchText))
                .OrderBy(s => s.Title)
                .Take(maxResults)
                .Select(s => new PublicationScheduleSeriesSuggestion(
                    s.SeriesId,
                    s.Title,
                    s.Slug,
                    s.CoverFile != null ? s.CoverFile.CloudinarySecureUrl : null))
                .ToListAsync(ct);

            return results.AsReadOnly();
        }

        public async Task<PublicationScheduleSeriesSuggestion?> GetSeriesSuggestionBySlugAsync(
            string slug,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            return await _dbContext.Series
                .AsNoTracking()
                .Where(s => s.StatusCode == SeriesStatusSerialized && s.Slug == slug)
                .Select(s => new PublicationScheduleSeriesSuggestion(
                    s.SeriesId,
                    s.Title,
                    s.Slug,
                    s.CoverFile != null ? s.CoverFile.CloudinarySecureUrl : null))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PublicationScheduleSeriesSuggestion?> GetSeriesSuggestionByIdAsync(
            Guid seriesId,
            CancellationToken ct = default)
        {
            if (seriesId == Guid.Empty)
                return null;

            return await _dbContext.Series
                .AsNoTracking()
                .Where(s => s.StatusCode == SeriesStatusSerialized && s.SeriesId == seriesId)
                .Select(s => new PublicationScheduleSeriesSuggestion(
                    s.SeriesId,
                    s.Title,
                    s.Slug,
                    s.CoverFile != null ? s.CoverFile.CloudinarySecureUrl : null))
                .FirstOrDefaultAsync(ct);
        }
    }
}
