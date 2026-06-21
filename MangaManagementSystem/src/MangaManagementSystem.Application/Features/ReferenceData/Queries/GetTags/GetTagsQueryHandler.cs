using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.ReferenceData.Queries.GetTags
{
    public sealed class GetTagsQueryHandler
        : IRequestHandler<GetTagsQuery, IReadOnlyList<TagDto>>
    {
        private readonly IReferenceDataRepository _repository;

        public GetTagsQueryHandler(IReferenceDataRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<TagDto>> Handle(
            GetTagsQuery request,
            CancellationToken cancellationToken)
        {
            var tags = await _repository.GetTagsAsync(cancellationToken);
            return tags
                .Select(t => new TagDto(t.TagId, t.TagName, t.Description))
                .ToList();
        }
    }
}
