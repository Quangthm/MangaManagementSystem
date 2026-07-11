using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CancelChapter;

/// <summary>
/// Handler for CancelChapterCommand.
/// </summary>
public sealed class CancelChapterCommandHandler
    : IRequestHandler<CancelChapterCommand, MangakaChapterListItemDto>
{
    private readonly IMangakaChapterRepository _repository;

    public CancelChapterCommandHandler(IMangakaChapterRepository repository)
    {
        _repository = repository;
    }

    public async Task<MangakaChapterListItemDto> Handle(
        CancelChapterCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty)
            throw new InvalidOperationException("A valid signed-in user is required.");

        if (request.ChapterId == Guid.Empty)
            throw new InvalidOperationException("A valid chapter is required.");

        return await _repository.CancelChapterAsync(
            request.ActorUserId,
            request.ChapterId,
            cancellationToken);
    }
}
