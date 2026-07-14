namespace MangaManagementSystem.API.Contracts.Ranking;

public sealed record UpdateSeriesVoteInputRequest(
    int RatingCount,
    decimal AverageRating,
    int ReadingCount,
    string? DataSourceNote);
