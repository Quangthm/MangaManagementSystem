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
        }
    }
}
