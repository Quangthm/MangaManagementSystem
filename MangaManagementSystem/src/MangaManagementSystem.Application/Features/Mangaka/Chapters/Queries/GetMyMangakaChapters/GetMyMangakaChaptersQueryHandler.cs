using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Queries.GetMyMangakaChapters
{
    /// <summary>
    /// Handler for GetMyMangakaChaptersQuery.
    /// </summary>
    public sealed class GetMyMangakaChaptersQueryHandler
        : IRequestHandler<GetMyMangakaChaptersQuery, IReadOnlyList<MangakaChapterListItemDto>>
    {
        private readonly IMangakaChapterRepository _repository;

        public GetMyMangakaChaptersQueryHandler(IMangakaChapterRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<MangakaChapterListItemDto>> Handle(
            GetMyMangakaChaptersQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                return Array.Empty<MangakaChapterListItemDto>();

            return await _repository.GetMyChaptersAsync(request.ActorUserId, cancellationToken);
        }
    }
}
