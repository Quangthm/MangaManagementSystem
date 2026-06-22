using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Queries.GetSeriesBySlug
{
    public sealed record GetSeriesBySlugQuery(
        string Slug,
        int ChapterPage,
        int ChapterPageSize) : IRequest<SeriesDetailDto?>;
}
