using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PutScheduledChapterOnHold;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PutScheduledChapterOnHold
{
    public sealed class PutScheduledChapterOnHoldCommandHandler
        : IRequestHandler<PutScheduledChapterOnHoldCommand, PutScheduledChapterOnHoldResponse>
    {
        private readonly IChapterOnHoldRepository _onHoldRepository;

        public PutScheduledChapterOnHoldCommandHandler(IChapterOnHoldRepository onHoldRepository)
        {
            _onHoldRepository = onHoldRepository;
        }

        public async Task<PutScheduledChapterOnHoldResponse> Handle(
            PutScheduledChapterOnHoldCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new InvalidOperationException("A reason is required to put a scheduled chapter on hold.");

            var result = await _onHoldRepository.PutScheduledChapterOnHoldAsync(
                request.ActorUserId,
                request.ChapterId,
                request.Reason,
                cancellationToken);

            return new PutScheduledChapterOnHoldResponse(
                result.ChapterId,
                result.StatusCode,
                result.Message);
        }
    }
}
