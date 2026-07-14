using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public class SeriesApiClient : ISeriesApiClient
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

        public async Task<SeriesLifecycleActionsDto> GetLifecycleActionsAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/series/{seriesId:D}/lifecycle-actions");

            return await SendLifecycleRequestAsync<SeriesLifecycleActionsDto>(
                requestMessage,
                seriesId,
                "Load lifecycle actions",
                "The available series actions could not be loaded.",
                cancellationToken);
        }

        public async Task<SeriesCompletionImpactDto> GetCompletionImpactAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/series/{seriesId:D}/completion-impact");

            return await SendLifecycleRequestAsync<SeriesCompletionImpactDto>(
                requestMessage,
                seriesId,
                "Load completion impact",
                "The completion impact could not be loaded.",
                cancellationToken);
        }

        public async Task<SeriesLifecycleChangedDto> SetHiatusAsync(
            Guid seriesId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (seriesId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Series id is required.",
                    nameof(seriesId));
            }

            var normalizedReason = reason?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                throw new ArgumentException(
                    "A hiatus reason is required.",
                    nameof(reason));
            }

            if (normalizedReason.Length > 500)
            {
                throw new ArgumentException(
                    "The hiatus reason cannot exceed 500 characters.",
                    nameof(reason));
            }

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/series/{seriesId:D}/hiatus")
            {
                Content = JsonContent.Create(
                    new
                    {
                        reason = normalizedReason
                    })
            };

            return await SendLifecycleRequestAsync<SeriesLifecycleChangedDto>(
                requestMessage,
                seriesId,
                "Set series hiatus",
                "The series could not be set to hiatus.",
                cancellationToken);
        }

        public async Task<SeriesLifecycleChangedDto> ResumeSerializationAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/series/{seriesId:D}/resume-serialization");

            return await SendLifecycleRequestAsync<SeriesLifecycleChangedDto>(
                requestMessage,
                seriesId,
                "Resume series serialization",
                "Serialization could not be resumed.",
                cancellationToken);
        }

        public async Task<SeriesLifecycleChangedDto> CompleteSeriesAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/series/{seriesId:D}/complete");

            return await SendLifecycleRequestAsync<SeriesLifecycleChangedDto>(
                requestMessage,
                seriesId,
                "Complete series",
                "The series could not be marked as completed.",
                cancellationToken);
        }

        private async Task<TResponse> SendLifecycleRequestAsync<TResponse>(
            HttpRequestMessage requestMessage,
            Guid seriesId,
            string operation,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            using var response = await _httpClient.SendAsync(
                requestMessage,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = await ExtractErrorMessageAsync(
                    response,
                    fallbackMessage,
                    cancellationToken);

                _logger.LogWarning(
                    "{Operation} failed for series {SeriesId}: {StatusCode} {ReasonPhrase}",
                    operation,
                    seriesId,
                    (int)response.StatusCode,
                    response.ReasonPhrase);

                throw new ApiClientException(
                    AuthErrorCodes.RequestFailed,
                    message,
                    response.StatusCode);
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(
                    cancellationToken: cancellationToken);

                return result
                    ?? throw new ApiClientException(
                        AuthErrorCodes.RequestFailed,
                        fallbackMessage,
                        response.StatusCode);
            }
            catch (JsonException)
            {
                _logger.LogWarning(
                    "{Operation} returned an invalid response for series {SeriesId}.",
                    operation,
                    seriesId);

                throw new ApiClientException(
                    AuthErrorCodes.RequestFailed,
                    fallbackMessage,
                    response.StatusCode);
            }
            catch (NotSupportedException)
            {
                _logger.LogWarning(
                    "{Operation} returned an unsupported response for series {SeriesId}.",
                    operation,
                    seriesId);

                throw new ApiClientException(
                    AuthErrorCodes.RequestFailed,
                    fallbackMessage,
                    response.StatusCode);
            }
        }

        private static async Task<string> ExtractErrorMessageAsync(
            HttpResponseMessage response,
            string fallbackMessage = "An unexpected error occurred. Please try again.",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(
                    cancellationToken);
                if (string.IsNullOrWhiteSpace(body))
                    return fallbackMessage;

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                {
                    var msg = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(msg))
                        return msg;
                }

                if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    var detail = detailProp.GetString();
                    if (!string.IsNullOrWhiteSpace(detail))
                        return detail;
                }
            }
            catch (JsonException)
            {
                // Ignore invalid JSON and fall through.
            }

            return fallbackMessage;
        }
    }
}
