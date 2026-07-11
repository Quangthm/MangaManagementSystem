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
    /// <summary>
    /// HttpClient-backed implementation of <see cref="IEditorDashboardApiClient"/>. Sends the
    /// transitional X-Actor-User-Id header and parses safe error messages for UI display.
    /// </summary>
    public sealed class EditorDashboardApiClient : BaseApiClient, IEditorDashboardApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly ILogger<EditorDashboardApiClient> _logger;

        public EditorDashboardApiClient(HttpClient httpClient, ILogger<EditorDashboardApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<EditorDashboardDto> GetDashboardAsync(
            Guid actorUserId, CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/editor/dashboard");
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var dashboard = await response.Content.ReadFromJsonAsync<EditorDashboardDto>(
                    cancellationToken: cancellationToken);

                if (dashboard is null)
                {
                    throw new InvalidOperationException(
                        "The dashboard returned no data. Please refresh and try again.");
                }

                return dashboard;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load editor dashboard failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }


    }
}
