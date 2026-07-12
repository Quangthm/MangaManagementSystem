using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Queries.GetEditorActionableChapters
{
    public sealed record GetEditorActionableChaptersQuery(
        Guid ActorUserId,
        Guid? SeriesId = null,
        string? SearchText = null,
        string? StatusCode = null,
        int MaxResults = 100) : IRequest<IReadOnlyList<EditorActionableChapterDto>>;
}
