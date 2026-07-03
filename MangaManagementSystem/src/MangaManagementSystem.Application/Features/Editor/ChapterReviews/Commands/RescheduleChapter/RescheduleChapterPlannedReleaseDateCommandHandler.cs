using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.RescheduleChapter;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.RescheduleChapter.Handlers
{
    public sealed class RescheduleChapterPlannedReleaseDateCommandHandler
        : IRequestHandler<RescheduleChapterPlannedReleaseDateCommand, RescheduleChapterResponse>
    {
        private readonly IEditorChapterReviewRepository _repository;

        public RescheduleChapterPlannedReleaseDateCommandHandler(IEditorChapterReviewRepository repository)
        {
            _repository = repository;
        }

        public async Task<RescheduleChapterResponse> Handle(
            RescheduleChapterPlannedReleaseDateCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new InvalidOperationException("A reason is required for rescheduling.");

            if (request.NewPlannedReleaseDate == default)
                throw new InvalidOperationException("A valid planned release date is required.");

            if (request.NewPlannedReleaseDate.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Planned release date cannot be in the past.");

            var result = await _repository.ReschedulePlannedReleaseDateAsync(
                request.ActorUserId,
                request.ChapterId,
                request.NewPlannedReleaseDate,
                request.Reason,
                cancellationToken);

            return new RescheduleChapterResponse(
                result.ChapterId,
                result.StatusCode,
                result.PlannedReleaseDate,
                result.Message);
        }
    }
}
