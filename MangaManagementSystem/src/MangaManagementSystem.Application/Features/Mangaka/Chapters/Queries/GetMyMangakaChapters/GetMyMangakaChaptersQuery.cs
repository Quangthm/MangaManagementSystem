using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Queries.GetMyMangakaChapters
{
    /// <summary>
    /// Query to get all chapters for series where the actor is an active Mangaka contributor.
    /// </summary>
    public sealed record GetMyMangakaChaptersQuery(Guid ActorUserId)
        : IRequest<IReadOnlyList<MangakaChapterListItemDto>>;
}
