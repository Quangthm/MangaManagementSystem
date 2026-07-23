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
    /// HttpClient-backed implementation of <see cref="IEditorDashboardApiClient"/>. Parses
    /// safe error messages for UI display.
    /// </summary>
    public class EditorDashboardApiClient : IEditorDashboardApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EditorDashboardApiClient> _logger;

        public EditorDashboardApiClient(HttpClient httpClient, ILogger<EditorDashboardApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<EditorDashboardDto> GetDashboardAsync(
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/editor/dashboard");

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

        private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return "An unexpected error occurred. Please try again.";
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                {
                    var msg = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(msg)) return msg;
                }

                if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    var detail = detailProp.GetString();
                    if (!string.IsNullOrWhiteSpace(detail)) return detail;
                }

                if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    var title = titleProp.GetString();
                    if (!string.IsNullOrWhiteSpace(title)) return title;
                }
            }
            catch (JsonException)
            {
                // Not valid JSON — fall through.
            }

            return "An unexpected error occurred. Please try again.";
        }
    }
}
