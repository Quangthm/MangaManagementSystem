using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.SetChapterPlannedReleaseDate
{
    public sealed class EditorSetChapterPlannedReleaseDateCommandHandler
        : IRequestHandler<EditorSetChapterPlannedReleaseDateCommand, SetChapterPlannedReleaseDateResponse>
    {
        private readonly IEditorChapterReviewRepository _repository;

        public EditorSetChapterPlannedReleaseDateCommandHandler(
            IEditorChapterReviewRepository repository)
        {
            _repository = repository;
        }

        public async Task<SetChapterPlannedReleaseDateResponse> Handle(
            EditorSetChapterPlannedReleaseDateCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            if (request.PlannedReleaseDate == default)
                throw new InvalidOperationException("A planned release date is required.");

            var result = await _repository.SetPlannedReleaseDateAsync(
                request.ActorUserId,
                request.ChapterId,
                request.PlannedReleaseDate,
                cancellationToken);

            return new SetChapterPlannedReleaseDateResponse(
                result.ChapterId,
                result.StatusCode,
                result.PlannedReleaseDate,
                result.Message,
                result.AllowedPeriodStart,
                result.AllowedPeriodEnd,
                result.FrequencyCode);
        }
    }
}
