using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CreateChapterDraft
{
    /// <summary>
    /// Command to create a new chapter draft with status DRAFT.
    /// </summary>
    public sealed record CreateChapterDraftCommand(
        Guid ActorUserId,
        Guid SeriesId,
        string ChapterNumberLabel,
        string? ChapterTitle) : IRequest<MangakaChapterListItemDto>;
}
