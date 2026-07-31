namespace MangaManagementSystem.Application.Features.Editor.Annotations.Models;

public sealed record EditorAnnotationSeriesGroup(
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    IReadOnlyList<EditorAnnotationRow> Annotations);
