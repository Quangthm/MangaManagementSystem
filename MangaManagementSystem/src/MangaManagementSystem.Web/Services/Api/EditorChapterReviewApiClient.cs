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
    /// HttpClient-backed implementation of <see cref="IEditorChapterReviewApiClient"/>. Sends
    /// the transitional X-Actor-User-Id header and parses safe error messages for UI display.
    /// </summary>
    public class EditorChapterReviewApiClient : IEditorChapterReviewApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly ILogger<EditorChapterReviewApiClient> _logger;

        public EditorChapterReviewApiClient(
            HttpClient httpClient, ILogger<EditorChapterReviewApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<EditorChapterReviewQueueDto> GetReviewQueueAsync(
            Guid actorUserId,
            string? statusFilter = null,
            CancellationToken cancellationToken = default)
        {
            var route = "api/editor/chapters/review-queue";
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                route += $"?status={Uri.EscapeDataString(statusFilter)}";
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, route);
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EditorChapterReviewQueueDto>(
                    cancellationToken: cancellationToken);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The chapter review queue returned no data. Please refresh and try again.");
                }

                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load chapter review queue failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<EditorChapterReviewDetailResult> GetReviewDetailAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Get, $"api/editor/chapters/{chapterId}/review-detail");
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadFromJsonAsync<EditorChapterReviewDetailDto>(
                    cancellationToken: cancellationToken);

                if (detail is null)
                {
                    return EditorChapterReviewDetailResult.Failure(
                        "The chapter review returned no data. Please refresh and try again.");
                }

                return EditorChapterReviewDetailResult.Success(detail);
            }

            // 403 (and 404, defensively) map to a friendly access-denied state without leaking details.
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return EditorChapterReviewDetailResult.Forbidden(
                    "You do not have access to this chapter review.");
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load chapter review detail {ChapterId} failed: {StatusCode} {ReasonPhrase}",
                chapterId, (int)response.StatusCode, response.ReasonPhrase);
            return EditorChapterReviewDetailResult.Failure(message);
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
