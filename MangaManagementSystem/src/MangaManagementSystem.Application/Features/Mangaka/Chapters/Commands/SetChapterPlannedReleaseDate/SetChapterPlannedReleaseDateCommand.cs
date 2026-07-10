using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.SetChapterPlannedReleaseDate
{
    public sealed record SetChapterPlannedReleaseDateCommand(
        Guid ActorUserId,
        Guid ChapterId,
        DateTime PlannedReleaseDate) : IRequest<SetChapterPlannedReleaseDateResponse>;
}
