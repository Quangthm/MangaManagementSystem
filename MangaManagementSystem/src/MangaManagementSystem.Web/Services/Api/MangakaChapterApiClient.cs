using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// HttpClient-backed implementation of <see cref="IMangakaChapterApiClient"/>.
    /// Sends the transitional X-Actor-User-Id header and parses safe error messages for UI display.
    /// </summary>
    public class MangakaChapterApiClient : IMangakaChapterApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly ILogger<MangakaChapterApiClient> _logger;

        public MangakaChapterApiClient(
            HttpClient httpClient,
            ILogger<MangakaChapterApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IReadOnlyList<MangakaChapterListItemDto>> GetMyChaptersAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/mangaka/chapters");
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<MangakaChapterListItemDto>>(
                    cancellationToken: cancellationToken);
                return result ?? new List<MangakaChapterListItemDto>();
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load my chapters failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<IReadOnlyList<MangakaChapterListItemDto>> GetSeriesChaptersAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Get, $"api/mangaka/series/{seriesId}/chapters");
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<MangakaChapterListItemDto>>(
                    cancellationToken: cancellationToken);
                return result ?? new List<MangakaChapterListItemDto>();
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load chapters for series {SeriesId} failed: {StatusCode} {ReasonPhrase}",
                seriesId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<MangakaChapterListItemDto> CreateChapterDraftAsync(
            Guid actorUserId,
            CreateChapterDraftRequest request,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/chapters")
            {
                Content = JsonContent.Create(request)
            };
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MangakaChapterListItemDto>(
                    cancellationToken: cancellationToken);
                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The chapter draft was created but no confirmation was returned. Please refresh and verify.");
                }
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Create chapter draft failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<MangakaChapterListItemDto> UpdateChapterDraftAsync(
            Guid actorUserId,
            Guid chapterId,
            UpdateChapterDraftRequest request,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Put, $"api/mangaka/chapters/{chapterId}")
            {
                Content = JsonContent.Create(request)
            };
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MangakaChapterListItemDto>(
                    cancellationToken: cancellationToken);
                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The chapter draft was updated but no confirmation was returned. Please refresh and verify.");
                }
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Update chapter draft {ChapterId} failed: {StatusCode} {ReasonPhrase}",
                chapterId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<MangakaChapterListItemDto> SubmitChapterForReviewAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/mangaka/chapters/{chapterId}/submit-review")
            {
                Content = JsonContent.Create(new { })
            };
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MangakaChapterListItemDto>(
                    cancellationToken: cancellationToken);
                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The chapter was submitted for review but no confirmation was returned. Please refresh and verify.");
                }
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Submit chapter {ChapterId} for review failed: {StatusCode} {ReasonPhrase}",
                chapterId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<MangakaChapterListItemDto> CancelChapterSubmissionAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/mangaka/chapters/{chapterId}/cancel-submission")
            {
                Content = JsonContent.Create(new { })
            };
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MangakaChapterListItemDto>(
                    cancellationToken: cancellationToken);
                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The submission was cancelled but no confirmation was returned. Please refresh and verify.");
                }
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Cancel submission for chapter {ChapterId} failed: {StatusCode} {ReasonPhrase}",
                chapterId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<MangakaChapterListItemDto> CancelChapterAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/mangaka/chapters/{chapterId}/cancel")
            {
                Content = JsonContent.Create(new { })
            };
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MangakaChapterListItemDto>(
                    cancellationToken: cancellationToken);
                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The chapter was cancelled but no confirmation was returned. Please refresh and verify.");
                }
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Cancel chapter {ChapterId} failed: {StatusCode} {ReasonPhrase}",
                chapterId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<MangakaChapterListItemDto> ScheduleApprovedChapterAsync(
            Guid actorUserId,
            Guid chapterId,
            ScheduleApprovedChapterRequest request,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/mangaka/chapters/{chapterId}/schedule")
            {
                Content = JsonContent.Create(request)
            };
            requestMessage.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MangakaChapterListItemDto>(
                    cancellationToken: cancellationToken);
                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The chapter was scheduled but no confirmation was returned. Please refresh and verify.");
                }
                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Schedule approved chapter {ChapterId} failed: {StatusCode} {ReasonPhrase}",
                chapterId, (int)response.StatusCode, response.ReasonPhrase);
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
