using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.Annotations.Queries.GetEditorAnnotations
{
    public sealed record GetEditorAnnotationsQuery(
        string? SeriesId,
        string? IssueType,
        string? Status,
        string ActorUserId) : IRequest<EditorAnnotationWorkspaceDto>;
}
