using System.Net.Http.Json;
using MangaManagementSystem.Application.Features.Ranking.Dtos;

namespace MangaManagementSystem.Web.Services.Api;

public sealed class SeriesRankingApiClient : ISeriesRankingApiClient
{
    private readonly HttpClient _httpClient;

    public SeriesRankingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PublicationPeriodDto>> GetWeeklyPeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/ranking/periods/weekly",
            cancellationToken);

        return await ReadListAsync<PublicationPeriodDto>(
            response,
            "The ranking periods could not be loaded.",
            cancellationToken);
    }

    public async Task<IReadOnlyList<SeriesVoteInputDto>> GetVoteInputsAsync(
        Guid publicationPeriodId,
        string? searchText,
        string? sort,
        CancellationToken cancellationToken = default)
    {
        var url = BuildPeriodUrl(
            publicationPeriodId,
            "vote-inputs",
            searchText,
            sort);

        var response = await _httpClient.GetAsync(url, cancellationToken);

        return await ReadListAsync<SeriesVoteInputDto>(
            response,
            "Series vote input could not be loaded.",
            cancellationToken);
    }

    public async Task<IReadOnlyList<SeriesRankingRowDto>> GetRankingAsync(
        Guid publicationPeriodId,
        string? searchText,
        string? sort,
        CancellationToken cancellationToken = default)
    {
        var url = BuildPeriodUrl(
            publicationPeriodId,
            "results",
            searchText,
            sort);

        var response = await _httpClient.GetAsync(url, cancellationToken);

        return await ReadListAsync<SeriesRankingRowDto>(
            response,
            "Series ranking results could not be loaded.",
            cancellationToken);
    }

    public async Task<IReadOnlyList<RankableSeriesDto>> SearchSeriesAsync(
        Guid publicationPeriodId,
        string? searchText,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/ranking/periods/{publicationPeriodId}/series-suggestions?maxResults={maxResults}";

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            url += $"&searchText={Uri.EscapeDataString(searchText.Trim())}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);

        return await ReadListAsync<RankableSeriesDto>(
            response,
            "Series suggestions could not be loaded.",
            cancellationToken);
    }

    public async Task<SeriesVoteInputDto> CreateVoteInputAsync(
        Guid publicationPeriodId,
        Guid seriesId,
        int ratingCount,
        decimal averageRating,
        int readingCount,
        string? dataSourceNote,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateSeriesVoteInputApiRequest(
            publicationPeriodId,
            seriesId,
            ratingCount,
            averageRating,
            readingCount,
            dataSourceNote);

        var response = await _httpClient.PostAsJsonAsync(
            "api/ranking/vote-inputs",
            request,
            cancellationToken);

        return await ReadRequiredAsync<SeriesVoteInputDto>(
            response,
            "Series vote input could not be created.",
            cancellationToken);
    }

    public async Task<SeriesVoteInputDto> UpdateVoteInputAsync(
        Guid seriesVoteInputId,
        int ratingCount,
        decimal averageRating,
        int readingCount,
        string? dataSourceNote,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateSeriesVoteInputApiRequest(
            ratingCount,
            averageRating,
            readingCount,
            dataSourceNote);

        var response = await _httpClient.PutAsJsonAsync(
            $"api/ranking/vote-inputs/{seriesVoteInputId}",
            request,
            cancellationToken);

        return await ReadRequiredAsync<SeriesVoteInputDto>(
            response,
            "Series vote input could not be updated.",
            cancellationToken);
    }

    private sealed record CreateSeriesVoteInputApiRequest(
        Guid PublicationPeriodId,
        Guid SeriesId,
        int RatingCount,
        decimal AverageRating,
        int ReadingCount,
        string? DataSourceNote);

    private sealed record UpdateSeriesVoteInputApiRequest(
        int RatingCount,
        decimal AverageRating,
        int ReadingCount,
        string? DataSourceNote);

    private static string BuildPeriodUrl(
        Guid publicationPeriodId,
        string resource,
        string? searchText,
        string? sort)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query.Add($"searchText={Uri.EscapeDataString(searchText.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            query.Add($"sort={Uri.EscapeDataString(sort.Trim())}");
        }

        var suffix = query.Count == 0
            ? string.Empty
            : "?" + string.Join("&", query);

        return $"api/ranking/periods/{publicationPeriodId}/{resource}{suffix}";
    }

    private static async Task<IReadOnlyList<T>> ReadListAsync<T>(
        HttpResponseMessage response,
        string emptyResponseMessage,
        CancellationToken cancellationToken)
    {
        var result = await ApiResponseReader.ReadRequiredAsync<List<T>>(
            response,
            emptyResponseMessage,
            cancellationToken);

        return result;
    }

    private static Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        string emptyResponseMessage,
        CancellationToken cancellationToken)
    {
        return ApiResponseReader.ReadRequiredAsync<T>(
            response,
            emptyResponseMessage,
            cancellationToken);
    }
}
