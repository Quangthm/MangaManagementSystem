using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CreateChapterDraft
{
    /// <summary>
    /// Handler for CreateChapterDraftCommand.
    /// </summary>
    public sealed class CreateChapterDraftCommandHandler
        : IRequestHandler<CreateChapterDraftCommand, MangakaChapterListItemDto>
    {
        private readonly IMangakaChapterRepository _repository;

        public CreateChapterDraftCommandHandler(IMangakaChapterRepository repository)
        {
            _repository = repository;
        }

        public async Task<MangakaChapterListItemDto> Handle(
            CreateChapterDraftCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.SeriesId == Guid.Empty)
                throw new InvalidOperationException("A valid series is required.");

            return await _repository.CreateChapterDraftAsync(
                request.ActorUserId,
                request.SeriesId,
                request.ChapterNumberLabel,
                request.ChapterTitle,
                cancellationToken);
        }
    }
}
