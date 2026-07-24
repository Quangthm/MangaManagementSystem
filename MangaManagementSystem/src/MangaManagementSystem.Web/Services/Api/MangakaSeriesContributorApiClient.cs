using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Web.Services.Api
{
    public class MangakaSeriesContributorApiClient : IMangakaSeriesContributorApiClient
    {
        private readonly HttpClient _httpClient;

        public MangakaSeriesContributorApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<IReadOnlyList<SeriesContributorListItemDto>> GetContributorsAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/series/{seriesId}/contributors");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<SeriesContributorListItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<SeriesContributorListItemDto>();
        }

        public async Task<IReadOnlyList<EligibleAssistantContributorDto>> SearchEligibleAssistantsAsync(
            Guid seriesId,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var url = $"api/mangaka/series/{seriesId}/contributors/eligible-assistants";
            if (!string.IsNullOrWhiteSpace(search))
            {
                url += $"?search={Uri.EscapeDataString(search)}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<EligibleAssistantContributorDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EligibleAssistantContributorDto>();
        }

        public async Task AddAssistantAsync(
            Guid seriesId,
            AddAssistantContributorRequest request,
            CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/series/{seriesId}/contributors/assistants");
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task EndAssistantAsync(
            Guid seriesId,
            Guid assistantUserId,
            EndAssistantContributorRequest request,
            CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/series/{seriesId}/contributors/assistants/{assistantUserId}/end");
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode}: {errorContent}",
                    null,
                    response.StatusCode);
            }
        }
    }
}
