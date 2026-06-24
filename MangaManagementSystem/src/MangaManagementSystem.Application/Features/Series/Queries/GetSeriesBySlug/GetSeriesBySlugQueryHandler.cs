using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Common.Policies;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Domain.ReadModels;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Queries.GetSeriesBySlug
{
    public sealed class GetSeriesBySlugQueryHandler
        : IRequestHandler<GetSeriesBySlugQuery, SeriesDetailDto?>
    {
        private readonly ISeriesRepository _seriesRepository;
        private readonly ISeriesProposalRepository _seriesProposalRepository;

        public GetSeriesBySlugQueryHandler(
            ISeriesRepository seriesRepository,
            ISeriesProposalRepository seriesProposalRepository)
        {
            _seriesRepository = seriesRepository;
            _seriesProposalRepository = seriesProposalRepository;
        }

        public async Task<SeriesDetailDto?> Handle(
            GetSeriesBySlugQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Slug))
                return null;

            int page = Math.Max(1, request.ChapterPage);
            int size = Math.Clamp(request.ChapterPageSize, 1, 50);

            var (series, contributors, chapters, totalChapterCount) =
                await _seriesRepository.GetSeriesDetailBySlugAsync(
                    request.Slug, page, size, cancellationToken);

            if (series is null)
                return null;

            var latestProposal = await _seriesProposalRepository
                .GetLatestBySeriesIdAsync(series.SeriesId, cancellationToken);

            if (!SeriesNavigationPolicy.CanOpenSeriesSlugPage(
                    series.StatusCode,
                    series.Slug,
                    latestProposal?.SeriesProposalId,
                    latestProposal?.StatusCode))
                return null;

            string? coverUrl = series.CoverFile?.DeletedAtUtc == null
                ? series.CoverFile?.CloudinarySecureUrl
                : null;

            int totalPages = totalChapterCount == 0
                ? 0
                : (int)Math.Ceiling((double)totalChapterCount / size);

            var contributorDtos = contributors
                .Select(c => new SeriesContributorSummaryDto(
                    c.DisplayName, c.RoleName, c.StartDate, c.EndDate))
                .ToList();

            var chapterDtos = chapters.Select(c => new SeriesChapterListItemDto(
                c.ChapterId,
                c.ChapterNumberLabel,
                c.ChapterTitle,
                c.StatusCode,
                c.PlannedReleaseDate,
                c.ReleasedAtUtc,
                c.CreatedAtUtc
            )).ToList();

            return new SeriesDetailDto(
                series.SeriesId,
                series.Slug,
                series.Title,
                series.Synopsis,
                MapGenres(series.Genres),
                MapTags(series.Tags),
                series.StatusCode,
                series.ContentLanguageCode,
                series.PublicationFrequencyCode,
                coverUrl,
                contributorDtos,
                chapterDtos,
                page,
                size,
                totalChapterCount,
                totalPages);
        }

        private static IReadOnlyList<GenreDto> MapGenres(IEnumerable<Domain.Entities.Genre> genres)
        {
            return genres
                .OrderBy(g => g.GenreName)
                .Select(g => new GenreDto(g.GenreId, g.GenreName))
                .ToList();
        }

        private static IReadOnlyList<TagDto> MapTags(IEnumerable<Domain.Entities.Tag> tags)
        {
            return tags
                .OrderBy(t => t.TagName)
                .Select(t => new TagDto(t.TagId, t.TagName))
                .ToList();
        }
    }
}
