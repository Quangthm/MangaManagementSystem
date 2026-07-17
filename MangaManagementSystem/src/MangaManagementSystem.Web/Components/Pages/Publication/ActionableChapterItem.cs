using System;

namespace MangaManagementSystem.Web.Components.Pages.Publication
{
    public sealed record ActionableChapterItem(
        Guid ChapterId,
        Guid SeriesId,
        string SeriesTitle,
        string ChapterNumberLabel,
        string? ChapterTitle,
        string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc,
        string? SeriesCoverUrl,
        bool CanRelease = false);

    public sealed record ScheduleChapterDialogResult(DateTime NewPlannedReleaseDate);

    public sealed record HoldChapterDialogResult(string Reason);
}
