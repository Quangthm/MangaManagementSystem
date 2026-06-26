using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.ScheduleApprovedChapter
{
    /// <summary>
    /// Command to schedule an approved chapter with a planned release date. Sets status to SCHEDULED.
    /// Only allowed when status is APPROVED.
    /// </summary>
    public sealed record ScheduleApprovedChapterCommand(
        Guid ActorUserId,
        Guid ChapterId,
        DateTime PlannedReleaseDate) : IRequest<MangakaChapterListItemDto>;
}
