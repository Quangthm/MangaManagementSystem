using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.SubmitChapterForReview
{
    /// <summary>
    /// Handler for SubmitChapterForReviewCommand.
    /// </summary>
    public sealed class SubmitChapterForReviewCommandHandler
        : IRequestHandler<SubmitChapterForReviewCommand, MangakaChapterListItemDto>
    {
        private readonly IMangakaChapterRepository _repository;

        public SubmitChapterForReviewCommandHandler(IMangakaChapterRepository repository)
        {
            _repository = repository;
        }

        public async Task<MangakaChapterListItemDto> Handle(
            SubmitChapterForReviewCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            return await _repository.SubmitChapterForReviewAsync(
                request.ActorUserId,
                request.ChapterId,
                cancellationToken);
        }
    }
}
