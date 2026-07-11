using MangaManagementSystem.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class MangakaSeriesContributorApiClient : BaseApiClient, IMangakaSeriesContributorApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;

        public MangakaSeriesContributorApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<IReadOnlyList<SeriesContributorListItemDto>> GetContributorsAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/series/{seriesId}/contributors");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<SeriesContributorListItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<SeriesContributorListItemDto>();
        }

        public async Task<IReadOnlyList<EligibleAssistantContributorDto>> SearchEligibleAssistantsAsync(
            Guid actorUserId,
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
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<EligibleAssistantContributorDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EligibleAssistantContributorDto>();
        }

        public async Task AddAssistantAsync(
            Guid actorUserId,
            Guid seriesId,
            AddAssistantContributorRequest request,
            CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/series/{seriesId}/contributors/assistants");
            httpRequest.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }
        }

        public async Task EndAssistantAsync(
            Guid actorUserId,
            Guid seriesId,
            Guid assistantUserId,
            EndAssistantContributorRequest request,
            CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/series/{seriesId}/contributors/assistants/{assistantUserId}/end");
            httpRequest.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }
        }


    }
}
