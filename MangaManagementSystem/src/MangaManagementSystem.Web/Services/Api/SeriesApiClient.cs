using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class SeriesApiClient : BaseApiClient, ISeriesApiClient
    {
        // Transitional actor header forwarded to the API while API auth is not yet implemented.
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly ILogger<SeriesApiClient> _logger;

        public SeriesApiClient(HttpClient httpClient, ILogger<SeriesApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<SeriesDetailDto?> GetSeriesDetailAsync(
            string slug,
            int chapterPage = 1,
            int chapterPageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var route = $"api/series/{Uri.EscapeDataString(slug)}?chapterPage={chapterPage}&chapterPageSize={chapterPageSize}";
            var response = await _httpClient.GetAsync(route, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SeriesDetailDto>(
                    cancellationToken: cancellationToken);
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load series detail failed for slug {Slug}: {StatusCode} {ReasonPhrase}",
                slug, (int)response.StatusCode, response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<SeriesWorkspaceEntryDto?> GetWorkspaceEntryAsync(
            Guid actorUserId,
            string slug,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/series/{Uri.EscapeDataString(slug)}/workspace-entry");
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SeriesWorkspaceEntryDto>(
                    cancellationToken: cancellationToken);
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load workspace entry failed for slug {Slug}: {StatusCode} {ReasonPhrase}",
                slug, (int)response.StatusCode, response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }


    }
}
