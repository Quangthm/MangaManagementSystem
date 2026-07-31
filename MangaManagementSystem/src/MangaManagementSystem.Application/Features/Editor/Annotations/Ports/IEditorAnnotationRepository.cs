using MangaManagementSystem.Application.Features.Editor.Annotations.Models;

namespace MangaManagementSystem.Application.Features.Editor.Annotations.Ports;

public interface IEditorAnnotationRepository
{
    Task<EditorAnnotationData> GetAnnotationsAsync(
        Guid actorUserId,
        Guid? seriesId,
        string? issueType,
        string? status,
        CancellationToken ct = default);
}
