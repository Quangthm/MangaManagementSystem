using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.ReleaseChapter
{
    public sealed class ReleaseChapterCommandHandler
        : IRequestHandler<ReleaseChapterCommand, ReleaseChapterResponse>
    {
        private readonly IChapterReleaseRepository _releaseRepository;

        public ReleaseChapterCommandHandler(IChapterReleaseRepository releaseRepository)
        {
            _releaseRepository = releaseRepository;
        }

        public async Task<ReleaseChapterResponse> Handle(
            ReleaseChapterCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            var result = await _releaseRepository.ReleaseChapterAsync(
                request.ActorUserId,
                request.ChapterId,
                request.ConfirmRelease,
                cancellationToken);

            return new ReleaseChapterResponse(
                result.ChapterId,
                result.StatusCode,
                result.ReleasedAtUtc,
                result.PlannedReleaseDate,
                result.Message);
        }
    }
}
