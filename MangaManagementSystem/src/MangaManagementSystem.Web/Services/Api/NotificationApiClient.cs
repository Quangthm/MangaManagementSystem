<<<<<<< HEAD
using MangaManagementSystem.Application.DTOs.Manga;
using System.Net.Http.Json;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class NotificationApiClient : BaseApiClient, INotificationApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public NotificationApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid actorUserId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/notifications");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<NotificationDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<NotificationDto>>(_jsonOptions, cancellationToken);
            return result ?? new List<NotificationDto>();
        }

        public async Task<NotificationDto?> MarkAsReadAsync(Guid actorUserId, Guid notificationId, CancellationToken cancellationToken = default)
        {
            if (notificationId == Guid.Empty)
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/notifications/{notificationId}/mark-read");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<NotificationDto>(_jsonOptions, cancellationToken);
=======
using System.Net;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class NotificationApiClient
        : INotificationApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationApiClient>
            _logger;

        public NotificationApiClient(
            HttpClient httpClient,
            ILogger<NotificationApiClient> logger)
        {
            _httpClient =
                httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));

            _logger =
                logger
                ?? throw new ArgumentNullException(
                    nameof(logger));
        }

        public async Task<IReadOnlyList<NotificationDto>>
            GetNotificationsAsync(
                int skip = 0,
                int take = 20,
                CancellationToken cancellationToken = default)
        {
            var normalizedSkip =
                Math.Max(
                    0,
                    skip);

            var normalizedTake =
                take < 1
                    ? 20
                    : Math.Min(take, 100);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "api/notifications?skip="
                    + normalizedSkip
                    + "&take="
                    + normalizedTake);

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Load notifications");

            var notifications =
                await ApiResponseReader
                    .ReadRequiredAsync<List<NotificationDto>>(
                        response,
                        "The Notification API returned an empty notification list.",
                        cancellationToken);

            return notifications;
        }

        public async Task<int> GetUnreadCountAsync(
            CancellationToken cancellationToken = default)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "api/notifications/unread-count");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Load unread notification count");

            var result =
                await ApiResponseReader
                    .ReadRequiredAsync<
                        UnreadNotificationCountDto>(
                            response,
                            "The Notification API returned an empty unread-count response.",
                            cancellationToken);

            return result.UnreadCount;
        }

        public async Task<bool> MarkAsReadAsync(
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            if (notificationId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Notification id is required.",
                    nameof(notificationId));
            }

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/notifications/{notificationId:D}/read");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (response.StatusCode ==
                HttpStatusCode.NotFound)
            {
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                LogFailure(
                    response,
                    "Mark notification as read");

                throw await ApiResponseReader
                    .CreateExceptionAsync(
                        response,
                        cancellationToken);
            }

            return true;
        }

        public async Task<MarkAllNotificationsReadResultDto>
            MarkAllAsReadAsync(
                CancellationToken cancellationToken = default)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/notifications/read-all");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Mark all notifications as read");

            return await ApiResponseReader
                .ReadRequiredAsync<
                    MarkAllNotificationsReadResultDto>(
                        response,
                        "The Notification API returned an empty mark-all response.",
                        cancellationToken);
        }

        private void LogFailure(
            HttpResponseMessage response,
            string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            _logger.LogWarning(
                "{Operation} failed: {StatusCode} {ReasonPhrase}",
                operation,
                (int)response.StatusCode,
                response.ReasonPhrase);
>>>>>>> main
        }
    }
}
