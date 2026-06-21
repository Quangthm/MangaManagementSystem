using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.ReferenceData.Queries.GetTags
{
    public sealed record GetTagsQuery : IRequest<IReadOnlyList<TagDto>>;
}
