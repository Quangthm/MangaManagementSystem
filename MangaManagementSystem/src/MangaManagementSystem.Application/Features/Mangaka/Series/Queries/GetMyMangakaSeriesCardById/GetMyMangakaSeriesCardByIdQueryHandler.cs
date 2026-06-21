using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Queries.GetMyMangakaSeriesCardById
{
    public sealed class GetMyMangakaSeriesCardByIdQueryHandler
        : IRequestHandler<GetMyMangakaSeriesCardByIdQuery, SeriesDto?>
    {
        private readonly ISeriesRepository _seriesRepository;

        public GetMyMangakaSeriesCardByIdQueryHandler(ISeriesRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        public async Task<SeriesDto?> Handle(
            GetMyMangakaSeriesCardByIdQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty || request.SeriesId == Guid.Empty)
                return null;

            var series = await _seriesRepository.GetByContributorAndSeriesIdAsync(
                request.ActorUserId,
                request.SeriesId,
                cancellationToken);

            if (series is null)
                return null;

            return MapToDto(series);
        }

        private static SeriesDto MapToDto(Domain.Entities.Series s) => new(
            s.SeriesId,
            s.Title,
            s.Slug,
            s.Synopsis,
            MapGenres(s.Genres),
            MapTags(s.Tags),
            s.CoverFileId,
            s.StatusCode,
            s.ContentLanguageCode,
            s.SourceSeriesId,
            s.CreatedAtUtc,
            s.UpdatedAtUtc,
            s.UpdatedByUserId,
            s.PublicationFrequencyCode,
            CoverUrl: s.CoverFile?.DeletedAtUtc == null
                ? s.CoverFile?.CloudinarySecureUrl
                : null
        );

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
