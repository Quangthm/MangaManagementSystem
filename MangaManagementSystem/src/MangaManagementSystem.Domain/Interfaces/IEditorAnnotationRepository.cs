using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IEditorAnnotationRepository
    {
        Task<EditorAnnotationData> GetAnnotationsAsync(
            Guid actorUserId,
            Guid? seriesId,
            string? issueType,
            string? status,
            CancellationToken ct = default);
    }

    public sealed record EditorAnnotationData(
        int OpenCount,
        int ResolvedCount,
        int PagesWithIssuesCount,
        int DistinctIssueTypeCount,
        IReadOnlyList<EditorAnnotationSeriesFilterItem> SeriesFilters,
        IReadOnlyList<string> IssueTypeFilters,
        IReadOnlyList<EditorAnnotationSeriesGroup> SeriesGroups);

    public sealed record EditorAnnotationSeriesFilterItem(
        Guid SeriesId,
        string SeriesTitle);

    public sealed record EditorAnnotationSeriesGroup(
        Guid SeriesId,
        string SeriesTitle,
        string? SeriesSlug,
        IReadOnlyList<EditorAnnotationRow> Annotations);

    public sealed record EditorAnnotationRow(
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
        IReadOnlyList<EditorAnnotationRegionItem> Regions);

    public sealed record EditorAnnotationRegionItem(
        Guid PageRegionId,
        string RegionTypeCode,
        decimal X,
        decimal Y,
        decimal Width,
        decimal Height);
}
