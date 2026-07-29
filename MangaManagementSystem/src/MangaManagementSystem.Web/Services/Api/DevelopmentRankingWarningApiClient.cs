using System.Net.Http.Json;
using MangaManagementSystem.Application.Features.Ranking.Warnings;

namespace MangaManagementSystem.Web.Services.Api;

public sealed class DevelopmentRankingWarningApiClient
    : IDevelopmentRankingWarningApiClient
{
    private readonly HttpClient _httpClient;

    public DevelopmentRankingWarningApiClient(
        HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<DevelopmentTimeState> GetTimeStateAsync(
        CancellationToken cancellationToken = default)
    {
        return GetRequiredAsync<DevelopmentTimeState>(
            "api/development/ranking-warning/time",
            cancellationToken);
    }

    public Task<DevelopmentTimeState> SetFixedUtcAsync(
        DateTimeOffset fixedUtc,
        CancellationToken cancellationToken = default)
    {
        return PostRequiredAsync<DevelopmentTimeState>(
            "api/development/ranking-warning/time/fixed",
            new { FixedUtc = fixedUtc },
            cancellationToken);
    }

    public Task<DevelopmentTimeState> ApplyOffsetAsync(
        double offsetMinutes,
        CancellationToken cancellationToken = default)
    {
        return PostRequiredAsync<DevelopmentTimeState>(
            "api/development/ranking-warning/time/offset",
            new { OffsetMinutes = offsetMinutes },
            cancellationToken);
    }

    public Task<DevelopmentTimeState> ResetTimeAsync(
        CancellationToken cancellationToken = default)
    {
        return PostRequiredAsync<DevelopmentTimeState>(
            "api/development/ranking-warning/time/reset",
            new { },
            cancellationToken);
    }

    public Task<RankingWarningEvaluationSummary> RunEvaluationAsync(
        CancellationToken cancellationToken = default)
    {
        return PostRequiredAsync<RankingWarningEvaluationSummary>(
            "api/development/ranking-warning/run",
            new { },
            cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(
        string requestUri,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            requestUri,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(
                   cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException(
                   "The API returned an empty response.");
    }

    private async Task<T> PostRequiredAsync<T>(
        string requestUri,
        object body,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            requestUri,
            body,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(
                   cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException(
                   "The API returned an empty response.");
    }
}
