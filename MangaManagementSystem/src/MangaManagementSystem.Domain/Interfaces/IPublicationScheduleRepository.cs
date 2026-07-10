using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IPublicationScheduleRepository
    {
        Task<IReadOnlyList<PublicationScheduleChapter>> GetScheduleChaptersAsync(
            DateTime weekStart,
            DateTime weekEnd,
            Guid? seriesId,
            string? frequencyCode,
            CancellationToken ct = default);

        Task<IReadOnlyList<PublicationScheduleSeriesSuggestion>> GetSeriesSuggestionsAsync(
            string searchText,
            int maxResults = 10,
            CancellationToken ct = default);
    }

    public sealed record PublicationScheduleSeriesSuggestion(
        Guid SeriesId,
        string SeriesTitle,
        string? SeriesCoverUrl);

    public sealed record PublicationScheduleChapter(
        Guid SeriesId,
        string SeriesTitle,
        string? SeriesSlug,
        string? SeriesCoverUrl,
        Guid ChapterId,
        string ChapterNumberLabel,
        string StatusCode,
        DateTime? PlannedReleaseDate,
        DateTime? ReleasedAtUtc,
        string? PublicationFrequencyCode);
}
