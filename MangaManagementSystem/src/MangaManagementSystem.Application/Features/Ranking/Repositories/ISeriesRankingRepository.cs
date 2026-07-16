using MangaManagementSystem.Application.Features.Ranking.Dtos;

namespace MangaManagementSystem.Application.Features.Ranking.Repositories;

public interface ISeriesRankingRepository
{
    Task<IReadOnlyList<PublicationPeriodDto>> GetWeeklyPublicationPeriodsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SeriesVoteInputDto>> GetSeriesVoteInputsAsync(
        Guid publicationPeriodId,
        string? searchText,
        string? sort,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SeriesRankingRowDto>> GetSeriesRankingAsync(
        Guid publicationPeriodId,
        string? searchText,
        string? sort,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RankableSeriesDto>> SearchRankableSeriesAsync(
        Guid publicationPeriodId,
        string? searchText,
        int maxResults,
        CancellationToken cancellationToken);

    Task<bool> IsVoteInputActorAsync(
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<SeriesVoteInputDto> CreateSeriesVoteInputAsync(
        Guid actorUserId,
        Guid publicationPeriodId,
        Guid seriesId,
        int ratingCount,
        decimal averageRating,
        int readingCount,
        string? dataSourceNote,
        CancellationToken cancellationToken);

    Task<SeriesVoteInputDto> UpdateSeriesVoteInputAsync(
        Guid actorUserId,
        Guid seriesVoteInputId,
        int ratingCount,
        decimal averageRating,
        int readingCount,
        string? dataSourceNote,
        CancellationToken cancellationToken);
}
