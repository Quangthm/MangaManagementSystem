using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.SetChapterPlannedReleaseDate;

public sealed record EditorSetChapterPlannedReleaseDateCommand(
    Guid ActorUserId,
    Guid ChapterId,
    DateTime PlannedReleaseDate) : IRequest<SetChapterPlannedReleaseDateResponse>;
