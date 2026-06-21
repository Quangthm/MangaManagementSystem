using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Queries.GetMyMangakaSeriesCardById
{
    public sealed record GetMyMangakaSeriesCardByIdQuery(
        Guid ActorUserId,
        Guid SeriesId)
        : IRequest<SeriesDto?>;
}
