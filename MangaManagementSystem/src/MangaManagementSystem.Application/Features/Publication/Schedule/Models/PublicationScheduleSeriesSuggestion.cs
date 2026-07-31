namespace MangaManagementSystem.Application.Features.Publication.Schedule.Models;

public sealed record PublicationScheduleSeriesSuggestion(
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    string? SeriesCoverUrl);
