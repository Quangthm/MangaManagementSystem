using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Queries.GetMangakaSeriesChapters
{
    /// <summary>
    /// Query to get chapters for a specific series where the actor is an active Mangaka contributor.
    /// </summary>
    public sealed record GetMangakaSeriesChaptersQuery(Guid ActorUserId, Guid SeriesId)
        : IRequest<IReadOnlyList<MangakaChapterListItemDto>>;
}
