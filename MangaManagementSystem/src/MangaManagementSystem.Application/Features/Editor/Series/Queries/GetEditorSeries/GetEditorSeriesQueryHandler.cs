using System;
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

        public GetEditorSeriesQueryHandler(IEditorSeriesRepository repository)
        {
            _repository = repository;
        }

        public async Task<EditorSeriesListDto> Handle(
            GetEditorSeriesQuery request, CancellationToken cancellationToken)
        {
            var series = await _repository.GetSeriesAsync(request.ActorUserId, cancellationToken);

            var dtos = series
                .Select(s => new EditorSeriesDto(
                    s.SeriesId,
                    s.Title,
                    s.Slug,
                    s.StatusCode,
                    s.CreatedAtUtc))
                .ToList();

            return new EditorSeriesListDto(dtos);
        }
    }
}
