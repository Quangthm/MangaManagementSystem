using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.ReferenceData.Queries.GetGenres
{
    public sealed record GetGenresQuery : IRequest<IReadOnlyList<GenreDto>>;
}
