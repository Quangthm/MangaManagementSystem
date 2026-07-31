using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Features.ChapterPages.Commands
{
    public record UpdateChapterPageCommand(UpdateChapterPageDto Dto) : IRequest<ChapterPageDto?>;
    public class UpdateChapterPageCommandHandler : IRequestHandler<UpdateChapterPageCommand, ChapterPageDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        public UpdateChapterPageCommandHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        public async Task<ChapterPageDto?> Handle(UpdateChapterPageCommand request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.ChapterPages.GetByIdAsync(request.Dto.ChapterPageId, cancellationToken);
            if (entity == null || entity.DeletedAtUtc != null) return null;

            entity.ChapterId = request.Dto.ChapterId;
            entity.PageNo = request.Dto.PageNo;
            entity.PageNotes = request.Dto.PageNotes;
            _unitOfWork.ChapterPages.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new ChapterPageDto(entity.ChapterPageId, entity.ChapterId, entity.PageNo, entity.PageNotes, entity.DeletedAtUtc, entity.DeletedByUserId);
        }
    }

    public record DeleteChapterPageCommand(Guid ChapterPageId, Guid? DeletedByUserId) : IRequest<bool>;
    public class DeleteChapterPageCommandHandler : IRequestHandler<DeleteChapterPageCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeleteChapterPageCommandHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        public async Task<bool> Handle(DeleteChapterPageCommand request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.ChapterPages.GetByIdAsync(request.ChapterPageId, cancellationToken);
            if (entity == null) throw new InvalidOperationException("DEBUG: The page could not be found in the database. It may have been hard-deleted.");
            if (entity.DeletedAtUtc != null) throw new InvalidOperationException($"DEBUG: The page is already soft-deleted! DeletedAtUtc={entity.DeletedAtUtc}");

            if (request.DeletedByUserId is null || request.DeletedByUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required to delete a page.");

            entity.DeletedAtUtc = DateTime.UtcNow;
            entity.DeletedByUserId = request.DeletedByUserId;
            _unitOfWork.ChapterPages.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
