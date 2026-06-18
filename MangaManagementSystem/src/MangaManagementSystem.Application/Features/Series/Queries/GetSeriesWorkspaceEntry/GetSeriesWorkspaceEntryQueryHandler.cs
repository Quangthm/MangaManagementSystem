using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Queries.GetSeriesWorkspaceEntry
{
    public sealed class GetSeriesWorkspaceEntryQueryHandler
        : IRequestHandler<GetSeriesWorkspaceEntryQuery, SeriesWorkspaceEntryDto?>
    {
        private readonly ISeriesRepository _seriesRepository;

        public GetSeriesWorkspaceEntryQueryHandler(ISeriesRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        public async Task<SeriesWorkspaceEntryDto?> Handle(
            GetSeriesWorkspaceEntryQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Slug) || request.ActorUserId == Guid.Empty)
                return null;

            var result = await _seriesRepository.GetWorkspaceEntryBySlugAsync(
                request.Slug, request.ActorUserId, cancellationToken);

            if (result is null)
                return null;

            var (seriesId, slug, title, canAccess) = result.Value;

            return new SeriesWorkspaceEntryDto(seriesId, slug, title, canAccess);
        }
    }
}
