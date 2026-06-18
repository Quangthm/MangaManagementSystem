using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Queries.GetSeriesBySlug
{
    public sealed class GetSeriesBySlugQueryHandler
        : IRequestHandler<GetSeriesBySlugQuery, SeriesDetailDto?>
    {
        private readonly ISeriesRepository _seriesRepository;

        public GetSeriesBySlugQueryHandler(ISeriesRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        public async Task<SeriesDetailDto?> Handle(
            GetSeriesBySlugQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Slug))
                return null;

            int page = Math.Max(1, request.ChapterPage);
            int size = Math.Clamp(request.ChapterPageSize, 1, 50);

            var (series, contributorNames, chapters, totalChapterCount) =
                await _seriesRepository.GetSeriesDetailBySlugAsync(
                    request.Slug, page, size, cancellationToken);

            if (series is null)
                return null;

            string? coverUrl = series.CoverFile?.DeletedAtUtc == null
                ? series.CoverFile?.CloudinarySecureUrl
                : null;

            int totalPages = totalChapterCount == 0
                ? 0
                : (int)Math.Ceiling((double)totalChapterCount / size);

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
                series.Genre,
                series.StatusCode,
                series.ContentLanguageCode,
                series.PublicationFrequencyCode,
                coverUrl,
                contributorNames,
                chapterDtos,
                page,
                size,
                totalChapterCount,
                totalPages);
        }
    }
}
