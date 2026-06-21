using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Queries.GetMyMangakaSeries
{
    public sealed class GetMyMangakaSeriesQueryHandler
        : IRequestHandler<GetMyMangakaSeriesQuery, IReadOnlyList<SeriesDto>>
    {
        private readonly ISeriesRepository _seriesRepository;

        public GetMyMangakaSeriesQueryHandler(ISeriesRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        public async Task<IReadOnlyList<SeriesDto>> Handle(
            GetMyMangakaSeriesQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                return Array.Empty<SeriesDto>();

            var entities = await _seriesRepository.GetByActiveContributorWithCoverAsync(
                request.ActorUserId, cancellationToken);

            return entities.Select(MapToDto).ToList();
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
