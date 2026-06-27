using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Queries.GetMangakaSeriesChapters
{
    /// <summary>
    /// Handler for GetMangakaSeriesChaptersQuery.
    /// </summary>
    public sealed class GetMangakaSeriesChaptersQueryHandler
        : IRequestHandler<GetMangakaSeriesChaptersQuery, IReadOnlyList<MangakaChapterListItemDto>>
    {
        private readonly IMangakaChapterRepository _repository;

        public GetMangakaSeriesChaptersQueryHandler(IMangakaChapterRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<MangakaChapterListItemDto>> Handle(
            GetMangakaSeriesChaptersQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty || request.SeriesId == Guid.Empty)
                return Array.Empty<MangakaChapterListItemDto>();

            return await _repository.GetSeriesChaptersAsync(request.ActorUserId, request.SeriesId, cancellationToken);
        }
    }
}
