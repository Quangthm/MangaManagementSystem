using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Read-only detail DTO for the /series/{slug} page.
    /// Includes series metadata, active contributor display names,
    /// and a paginated chapter list.
    /// </summary>
    public sealed record SeriesDetailDto(
        Guid SeriesId,
        string Slug,
        string Title,
        string Synopsis,
        string Genre,
        string StatusCode,
        string ContentLanguageCode,
        string? PublicationFrequencyCode,
        string? CoverUrl,
        IReadOnlyList<string> ContributorDisplayNames,
        IReadOnlyList<SeriesChapterListItemDto> Chapters,
        int ChapterPage,
        int ChapterPageSize,
        int TotalChapterCount,
        int TotalChapterPages);

    /// <summary>
    /// Lightweight chapter summary for series detail page chapter list.
    /// Maps only fields that exist on the Chapter entity.
    /// </summary>
    public sealed record SeriesChapterListItemDto(
        Guid ChapterId,
        string ChapterNumberLabel,
        string? Title,
        string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc,
        DateTime CreatedAtUtc);

    /// <summary>
    /// Workspace entry access check DTO. Used by both /series/{slug} (to
    /// enable/disable the Open Workspace button) and /series/{slug}/workspace
    /// (to enforce series-specific access before loading workspace content).
    /// </summary>
    public sealed record SeriesWorkspaceEntryDto(
        Guid SeriesId,
        string Slug,
        string Title,
        bool CanAccess);
}
