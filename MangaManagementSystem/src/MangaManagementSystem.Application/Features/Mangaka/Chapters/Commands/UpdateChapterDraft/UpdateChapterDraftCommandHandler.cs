using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.UpdateChapterDraft
{
    /// <summary>
    /// Handler for UpdateChapterDraftCommand.
    /// </summary>
    public sealed class UpdateChapterDraftCommandHandler
        : IRequestHandler<UpdateChapterDraftCommand, MangakaChapterListItemDto>
    {
        private readonly IMangakaChapterRepository _repository;

        public UpdateChapterDraftCommandHandler(IMangakaChapterRepository repository)
        {
            _repository = repository;
        }

        public async Task<MangakaChapterListItemDto> Handle(
            UpdateChapterDraftCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            return await _repository.UpdateChapterDraftAsync(
                request.ActorUserId,
                request.ChapterId,
                request.ChapterNumberLabel,
                request.ChapterTitle,
                cancellationToken);
        }
    }
}
