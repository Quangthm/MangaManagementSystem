using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.Series.Queries.GetEditorSeries
{
    public sealed class GetEditorSeriesQueryHandler
        : IRequestHandler<GetEditorSeriesQuery, EditorSeriesListDto>
    {
        private readonly IEditorSeriesRepository _repository;
        private readonly ISeriesProposalRepository _seriesProposalRepository;

        public GetEditorSeriesQueryHandler(
            IEditorSeriesRepository repository,
            ISeriesProposalRepository seriesProposalRepository)
        {
            _repository = repository;
            _seriesProposalRepository = seriesProposalRepository;
        }

        public async Task<EditorSeriesListDto> Handle(
            GetEditorSeriesQuery request, CancellationToken cancellationToken)
        {
            var seriesList = await _repository.GetSeriesAsync(request.ActorUserId, cancellationToken);

            if (seriesList.Count == 0)
                return new EditorSeriesListDto(new List<EditorSeriesDto>());

            var seriesIds = seriesList.Select(s => s.SeriesId).ToList();

            var latestProposals = await _seriesProposalRepository.GetLatestForSeriesBatchAsync(
                seriesIds, cancellationToken);

            var latestBySeriesId = latestProposals
                .GroupBy(p => p.SeriesId)
                .ToDictionary(g => g.Key, g => g.First());

            var dtos = seriesList
                .Select(s =>
                {
                    latestBySeriesId.TryGetValue(s.SeriesId, out var latest);
                    return new EditorSeriesDto(
                        s.SeriesId,
                        s.Title,
                        s.Slug,
                        s.StatusCode,
                        s.CreatedAtUtc,
                        latest?.SeriesProposalId,
                        latest?.StatusCode);
                })
                .ToList();

            return new EditorSeriesListDto(dtos);
        }
    }
}
