using MangaManagementSystem.Application.Features.Publication.Schedule.Models;

namespace MangaManagementSystem.Application.Features.Publication.Schedule.Ports;

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

    Task<PublicationScheduleSeriesSuggestion?> GetSeriesSuggestionBySlugAsync(
        string slug,
        CancellationToken ct = default);

    Task<PublicationScheduleSeriesSuggestion?> GetSeriesSuggestionByIdAsync(
        Guid seriesId,
        CancellationToken ct = default);
}
