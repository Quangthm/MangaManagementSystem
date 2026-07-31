namespace MangaManagementSystem.Application.Features.Editor.Annotations.Models;

public sealed record EditorAnnotationData(
    int OpenCount,
    int ResolvedCount,
    int PagesWithIssuesCount,
    int DistinctIssueTypeCount,
    IReadOnlyList<EditorAnnotationSeriesFilterItem> SeriesFilters,
    IReadOnlyList<string> IssueTypeFilters,
    IReadOnlyList<EditorAnnotationSeriesGroup> SeriesGroups);
