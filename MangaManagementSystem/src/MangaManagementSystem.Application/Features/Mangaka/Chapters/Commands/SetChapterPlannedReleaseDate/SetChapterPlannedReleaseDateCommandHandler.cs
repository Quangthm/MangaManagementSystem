using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Services;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.SetChapterPlannedReleaseDate;

public sealed class SetChapterPlannedReleaseDateCommandHandler
    : IRequestHandler<SetChapterPlannedReleaseDateCommand, SetChapterPlannedReleaseDateResponse>
{
    private readonly IMangakaChapterRepository _repository;
    private readonly ChapterSchedulingValidator _schedulingValidator;

    public SetChapterPlannedReleaseDateCommandHandler(
        IMangakaChapterRepository repository,
        ChapterSchedulingValidator schedulingValidator)
    {
        _repository = repository;
        _schedulingValidator = schedulingValidator;
    }

    public async Task<SetChapterPlannedReleaseDateResponse> Handle(
        SetChapterPlannedReleaseDateCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty)
            throw new InvalidOperationException("A valid signed-in user is required.");

        if (request.ChapterId == Guid.Empty)
            throw new InvalidOperationException("A valid chapter is required.");

        if (request.PlannedReleaseDate == default)
            throw new InvalidOperationException("A planned release date is required.");

        return await _repository.SetPlannedReleaseDateAsync(
            request.ActorUserId,
            request.ChapterId,
            request.PlannedReleaseDate,
            _schedulingValidator,
            cancellationToken);
    }
}
