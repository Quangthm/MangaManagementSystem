using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.ReferenceData.Queries.GetGenres
{
    public sealed class GetGenresQueryHandler
        : IRequestHandler<GetGenresQuery, IReadOnlyList<GenreDto>>
    {
        private readonly IReferenceDataRepository _repository;

        public GetGenresQueryHandler(IReferenceDataRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<GenreDto>> Handle(
            GetGenresQuery request,
            CancellationToken cancellationToken)
        {
            var genres = await _repository.GetGenresAsync(cancellationToken);
            return genres
                .Select(g => new GenreDto(g.GenreId, g.GenreName, g.Description))
                .ToList();
        }
    }
}
