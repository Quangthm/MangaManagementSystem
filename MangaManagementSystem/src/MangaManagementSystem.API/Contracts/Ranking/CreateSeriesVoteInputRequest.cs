namespace MangaManagementSystem.API.Contracts.Ranking;

public sealed record CreateSeriesVoteInputRequest(
    Guid PublicationPeriodId,
    Guid SeriesId,
    int RatingCount,
    decimal AverageRating,
    int ReadingCount,
    string? DataSourceNote);
