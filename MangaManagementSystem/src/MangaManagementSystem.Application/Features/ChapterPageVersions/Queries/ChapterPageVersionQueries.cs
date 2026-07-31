using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Features.ChapterPageVersions.Queries
{
    public record GetChapterPageVersionsByPageIdsQuery(IEnumerable<Guid> ChapterPageIds) : IRequest<IEnumerable<ChapterPageVersionDto>>;
    public class GetChapterPageVersionsByPageIdsQueryHandler : IRequestHandler<GetChapterPageVersionsByPageIdsQuery, IEnumerable<ChapterPageVersionDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetChapterPageVersionsByPageIdsQueryHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        public async Task<IEnumerable<ChapterPageVersionDto>> Handle(GetChapterPageVersionsByPageIdsQuery request, CancellationToken cancellationToken)
        {
            var versions = await _unitOfWork.ChapterPageVersions.GetByPageIdsAsync(request.ChapterPageIds, cancellationToken);
            return versions.OrderBy(v => v.VersionNo).Select(v => new ChapterPageVersionDto(
                v.ChapterPageVersionId, v.ChapterPageId, v.VersionNo, v.PageFileId, v.VersionNote, v.IsCurrentVersion));
        }
    }

    public record GetChapterPageVersionByIdQuery(Guid ChapterPageVersionId) : IRequest<ChapterPageVersionDto?>;
    public class GetChapterPageVersionByIdQueryHandler : IRequestHandler<GetChapterPageVersionByIdQuery, ChapterPageVersionDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetChapterPageVersionByIdQueryHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        public async Task<ChapterPageVersionDto?> Handle(GetChapterPageVersionByIdQuery request, CancellationToken cancellationToken)
        {
            var v = await _unitOfWork.ChapterPageVersions.GetByIdAsync(request.ChapterPageVersionId, cancellationToken);
            return v == null ? null : new ChapterPageVersionDto(
                v.ChapterPageVersionId, v.ChapterPageId, v.VersionNo, v.PageFileId, v.VersionNote, v.IsCurrentVersion);
        }
    }
}
