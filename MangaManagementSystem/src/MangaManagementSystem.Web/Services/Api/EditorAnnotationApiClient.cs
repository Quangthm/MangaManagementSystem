using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public class EditorAnnotationApiClient : IEditorAnnotationApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EditorAnnotationApiClient> _logger;

        public EditorAnnotationApiClient(
            HttpClient httpClient,
            ILogger<EditorAnnotationApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<EditorAnnotationWorkspaceDto> GetAnnotationsAsync(
            string? seriesId = null,
            string? issueType = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            var route = "api/editor/annotations";

            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(seriesId))
                queryParams.Add($"seriesId={Uri.EscapeDataString(seriesId)}");
            if (!string.IsNullOrWhiteSpace(issueType))
                queryParams.Add($"issueType={Uri.EscapeDataString(issueType)}");
            if (!string.IsNullOrWhiteSpace(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");

            if (queryParams.Count > 0)
                route += "?" + string.Join("&", queryParams);

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, route);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EditorAnnotationWorkspaceDto>(
                    cancellationToken: cancellationToken);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The annotations data returned no data. Please refresh and try again.");
                }

                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load editor annotations failed: {StatusCode} {ReasonPhrase}",
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
            }

            return "An unexpected error occurred. Please try again.";
        }
    }
}
