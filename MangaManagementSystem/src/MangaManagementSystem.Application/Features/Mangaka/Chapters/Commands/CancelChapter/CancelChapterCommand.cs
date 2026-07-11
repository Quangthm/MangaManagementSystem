using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Chapters.Commands.CancelChapter;

/// <summary>
/// Command to cancel a chapter. Preserves the chapter and its content, sets status to CANCELLED,
/// and writes an audit event. A cancelled chapter does not reserve its chapter number (BR-CH-002).
/// </summary>
public sealed record CancelChapterCommand(
    Guid ActorUserId,
    Guid ChapterId) : IRequest<MangakaChapterListItemDto>;
