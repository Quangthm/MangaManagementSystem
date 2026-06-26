using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.ScheduleApprovedChapter
{
    /// <summary>
    /// Handler for ScheduleApprovedChapterCommand.
    /// </summary>
    public sealed class ScheduleApprovedChapterCommandHandler
        : IRequestHandler<ScheduleApprovedChapterCommand, MangakaChapterListItemDto>
    {
        private readonly IMangakaChapterRepository _repository;

        public ScheduleApprovedChapterCommandHandler(IMangakaChapterRepository repository)
        {
            _repository = repository;
        }

        public async Task<MangakaChapterListItemDto> Handle(
            ScheduleApprovedChapterCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            if (request.PlannedReleaseDate == default)
                throw new InvalidOperationException("A planned release date is required.");

            if (request.PlannedReleaseDate.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Planned release date cannot be in the past.");

            return await _repository.ScheduleApprovedChapterAsync(
                request.ActorUserId,
                request.ChapterId,
                request.PlannedReleaseDate,
                cancellationToken);
        }
    }
}
