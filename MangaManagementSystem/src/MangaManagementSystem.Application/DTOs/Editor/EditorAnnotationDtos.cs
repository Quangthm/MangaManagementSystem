using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Editor
{
    public sealed record EditorAnnotationWorkspaceDto(
        int OpenCount,
        int ResolvedCount,
        int PagesWithIssuesCount,
        int DistinctIssueTypeCount,
        IReadOnlyList<EditorAnnotationSeriesFilterDto> SeriesFilters,
        IReadOnlyList<string> IssueTypeFilters,
        IReadOnlyList<EditorAnnotationSeriesGroupDto> SeriesGroups);

    public sealed record EditorAnnotationSeriesFilterDto(
        Guid SeriesId,
        string SeriesTitle);

    public sealed record EditorAnnotationSeriesGroupDto(
        Guid SeriesId,
        string SeriesTitle,
        string? SeriesSlug,
        IReadOnlyList<EditorAnnotationRowDto> Annotations);

    public sealed record EditorAnnotationRowDto(
        Guid AnnotationId,
        Guid ChapterId,
        string ChapterNumberLabel,
        string? ChapterTitle,
        Guid ChapterPageId,
        int PageNumber,
        Guid ChapterPageVersionId,
        short? VersionNo,
        string IssueTypeCode,
        string? AnnotationText,
        bool IsResolved,
        DateTime CreatedAtUtc,
        DateTime? ResolvedAtUtc,
        string? WorkspaceUrl,
        IReadOnlyList<EditorAnnotationRegionDto> Regions);

    public sealed record EditorAnnotationRegionDto(
        Guid PageRegionId,
        string RegionTypeCode,
        decimal X,
        decimal Y,
        decimal Width,
        decimal Height);
}
