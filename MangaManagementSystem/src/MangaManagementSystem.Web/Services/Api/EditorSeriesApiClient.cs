using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class EditorSeriesApiClient : BaseApiClient, IEditorSeriesApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly ILogger<EditorSeriesApiClient> _logger;

        public EditorSeriesApiClient(
            HttpClient httpClient,
            ILogger<EditorSeriesApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<EditorSeriesListDto> GetSeriesAsync(
            Guid actorUserId, CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/editor/series");
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EditorSeriesListDto>(
                    cancellationToken: cancellationToken);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The series library data returned no data. Please refresh and try again.");
                }

                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load editor series library failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }


    }
}
