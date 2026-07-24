using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.ReleaseChapter;
using MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PutScheduledChapterOnHold;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// HttpClient-backed implementation of <see cref="IEditorChapterReviewApiClient"/>. Parses
    /// safe error messages for UI display.
    /// </summary>
    public class EditorChapterReviewApiClient : IEditorChapterReviewApiClient
    {
        private const long MaxMarkupFileSize = 10 * 1024 * 1024; // 10 MB

        private readonly HttpClient _httpClient;
        private readonly ILogger<EditorChapterReviewApiClient> _logger;

        public EditorChapterReviewApiClient(
            HttpClient httpClient, ILogger<EditorChapterReviewApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<EditorChapterReviewQueueDto> GetReviewQueueAsync(
            string? statusFilter = null,
            CancellationToken cancellationToken = default)
        {
            var route = "api/editor/chapters/review-queue";
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                route += $"?status={Uri.EscapeDataString(statusFilter)}";
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, route);

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
            Guid chapterId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Get, $"api/editor/chapters/{chapterId}/review-detail");
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

        public async Task<SubmitChapterEditorialReviewResponse> SubmitReviewDecisionWithMarkupAsync(
            Guid chapterId,
            string decisionCode,
            string? comments,
            IBrowserFile? markupFile,
            CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(decisionCode), nameof(decisionCode));
            if (!string.IsNullOrWhiteSpace(comments))
            {
                content.Add(new StringContent(comments), nameof(comments));
            }

            if (markupFile is not null)
            {
                var fileContent = new StreamContent(markupFile.OpenReadStream(MaxMarkupFileSize));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(markupFile.ContentType);
                content.Add(fileContent, "MarkupFile", markupFile.Name);
            }

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/editor/chapters/{chapterId}/review-decision/with-markup")
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<SubmitChapterEditorialReviewResponse>(
                        cancellationToken: cancellationToken);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The review decision returned no data. Please refresh and try again.");
                }

                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Submit review decision with markup for chapter {ChapterId} failed: {StatusCode} {ReasonPhrase}",
                chapterId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<SubmitChapterEditorialReviewResponse> SubmitReviewDecisionAsync(
            Guid chapterId,
            SubmitChapterEditorialReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/editor/chapters/{chapterId}/review-decision")
            {
                Content = JsonContent.Create(request)
            };
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<SubmitChapterEditorialReviewResponse>(
                        cancellationToken: cancellationToken);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The review decision returned no data. Please refresh and try again.");
                }

                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Submit review decision for chapter {ChapterId} failed: {StatusCode} {ReasonPhrase}",
                chapterId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<PutScheduledChapterOnHoldResponse> PutChapterOnHoldAsync(
            Guid chapterId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var request = new { reason };
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/editor/chapters/{chapterId}/hold")
            {
                Content = JsonContent.Create(request)
            };
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PutScheduledChapterOnHoldResponse>(
                    cancellationToken: cancellationToken);
                if (result is null)
                    throw new InvalidOperationException("No confirmation was returned. Please refresh and verify.");
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            throw new InvalidOperationException(message);
        }

        public async Task<SetChapterPlannedReleaseDateResponse> SetPlannedReleaseDateAsync(
            Guid chapterId,
            SetPlannedReleaseDateRequest request,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Put, $"api/editor/chapters/{chapterId}/planned-release-date")
            {
                Content = JsonContent.Create(request)
            };
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SetChapterPlannedReleaseDateResponse>(
                    cancellationToken: cancellationToken);
                if (result is null)
                    throw new InvalidOperationException("No confirmation was returned. Please refresh and verify.");
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            throw new InvalidOperationException(message);
        }

        public async Task<ReleaseChapterResponse> ReleaseChapterAsync(
            Guid chapterId,
            bool confirmRelease,
            CancellationToken cancellationToken = default)
        {
            var request = new { confirmRelease };
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/editor/chapters/{chapterId}/release")
            {
                Content = JsonContent.Create(request)
            };
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ReleaseChapterResponse>(
                    cancellationToken: cancellationToken);
                if (result is null)
                    throw new InvalidOperationException("No confirmation was returned. Please refresh and verify.");
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            throw new InvalidOperationException(message);
        }

        public async Task<IReadOnlyList<EditorActionableChapterDto>> GetActionableChaptersAsync(
            Guid? seriesId = null,
            string? searchText = null,
            string? statusCode = null,
            int? maxResults = null,
            CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder("api/editor/chapters/series-chapters?");
            var hasParam = false;

            if (seriesId.HasValue && seriesId.Value != Guid.Empty)
            {
                sb.Append($"seriesId={seriesId.Value}");
                hasParam = true;
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (hasParam) sb.Append('&');
                sb.Append($"searchText={Uri.EscapeDataString(searchText)}");
                hasParam = true;
            }

            if (!string.IsNullOrWhiteSpace(statusCode))
            {
                if (hasParam) sb.Append('&');
                sb.Append($"statusCode={Uri.EscapeDataString(statusCode)}");
                hasParam = true;
            }

            if (maxResults.HasValue)
            {
                if (hasParam) sb.Append('&');
                sb.Append($"maxResults={maxResults.Value}");
                hasParam = true;
            }

            if (!hasParam)
                sb.Length -= 1;

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, sb.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<EditorActionableChapterDto>>(
                    cancellationToken: cancellationToken);
                return result?.AsReadOnly() ?? (IReadOnlyList<EditorActionableChapterDto>)Array.Empty<EditorActionableChapterDto>();
            }

            var errorMessage = await ExtractErrorMessageAsync(response);
            throw new InvalidOperationException(errorMessage);
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
