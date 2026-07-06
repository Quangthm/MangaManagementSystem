using MangaManagementSystem.Application.Features.Ranking.Dtos;

namespace MangaManagementSystem.Web.Services.Api;

public interface ISeriesRankingApiClient
{
    Task<IReadOnlyList<PublicationPeriodDto>> GetWeeklyPeriodsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SeriesVoteInputDto>> GetVoteInputsAsync(
        Guid publicationPeriodId,
        string? searchText,
        string? sort,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SeriesRankingRowDto>> GetRankingAsync(
        Guid publicationPeriodId,
        string? searchText,
        string? sort,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankableSeriesDto>> SearchSeriesAsync(
        Guid publicationPeriodId,
        string? searchText,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    Task<SeriesVoteInputDto> CreateVoteInputAsync(
        Guid publicationPeriodId,
        Guid seriesId,
        int ratingCount,
        decimal averageRating,
        int readingCount,
        string? dataSourceNote,
        CancellationToken cancellationToken = default);

    Task<SeriesVoteInputDto> UpdateVoteInputAsync(
        Guid seriesVoteInputId,
        int ratingCount,
        decimal averageRating,
        int readingCount,
        string? dataSourceNote,
        CancellationToken cancellationToken = default);
}
