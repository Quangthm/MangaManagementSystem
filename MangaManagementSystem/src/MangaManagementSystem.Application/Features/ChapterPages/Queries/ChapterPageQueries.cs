using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Features.ChapterPages.Queries
{
    public record GetChapterPagesByChapterIdQuery(Guid ChapterId) : IRequest<IEnumerable<ChapterPageDto>>;
    public class GetChapterPagesByChapterIdQueryHandler : IRequestHandler<GetChapterPagesByChapterIdQuery, IEnumerable<ChapterPageDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetChapterPagesByChapterIdQueryHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        public async Task<IEnumerable<ChapterPageDto>> Handle(GetChapterPagesByChapterIdQuery request, CancellationToken cancellationToken)
        {
            var pages = await _unitOfWork.ChapterPages.GetByChapterIdAsync(request.ChapterId, cancellationToken);
            return pages.OrderBy(p => p.PageNo).Select(p => new ChapterPageDto(p.ChapterPageId, p.ChapterId, p.PageNo, p.PageNotes, p.DeletedAtUtc, p.DeletedByUserId));
        }
    }

    public record GetChapterPageByIdQuery(Guid ChapterPageId) : IRequest<ChapterPageDto?>;
    public class GetChapterPageByIdQueryHandler : IRequestHandler<GetChapterPageByIdQuery, ChapterPageDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetChapterPageByIdQueryHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        public async Task<ChapterPageDto?> Handle(GetChapterPageByIdQuery request, CancellationToken cancellationToken)
        {
            var p = await _unitOfWork.ChapterPages.GetByIdAsync(request.ChapterPageId, cancellationToken);
            return p == null ? null : new ChapterPageDto(p.ChapterPageId, p.ChapterId, p.PageNo, p.PageNotes, p.DeletedAtUtc, p.DeletedByUserId);
        }
    }

    public record GetPageCountsByChapterIdsQuery(IEnumerable<Guid> ChapterIds) : IRequest<Dictionary<Guid, int>>;
    public class GetPageCountsByChapterIdsQueryHandler : IRequestHandler<GetPageCountsByChapterIdsQuery, Dictionary<Guid, int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetPageCountsByChapterIdsQueryHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        public Task<Dictionary<Guid, int>> Handle(GetPageCountsByChapterIdsQuery request, CancellationToken cancellationToken)
        {
            return _unitOfWork.ChapterPages.GetPageCountsByChapterIdsAsync(request.ChapterIds, cancellationToken);
        }
    }
}
