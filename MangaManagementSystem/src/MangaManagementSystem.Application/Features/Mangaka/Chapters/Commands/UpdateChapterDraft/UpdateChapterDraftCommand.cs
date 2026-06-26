using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.UpdateChapterDraft
{
    /// <summary>
    /// Command to update chapter draft metadata. Only allowed when status is DRAFT or REVISION_REQUESTED.
    /// </summary>
    public sealed record UpdateChapterDraftCommand(
        Guid ActorUserId,
        Guid ChapterId,
        string ChapterNumberLabel,
        string? ChapterTitle) : IRequest<MangakaChapterListItemDto>;
}
