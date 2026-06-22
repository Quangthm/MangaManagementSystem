using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Queries.GetMyMangakaSeries
{
    public sealed record GetMyMangakaSeriesQuery(Guid ActorUserId)
        : IRequest<IReadOnlyList<SeriesDto>>;
}
