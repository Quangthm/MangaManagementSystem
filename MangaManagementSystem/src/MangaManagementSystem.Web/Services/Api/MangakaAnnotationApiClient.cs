using MangaManagementSystem.Application.DTOs.Manga;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public class MangakaAnnotationApiClient : IMangakaAnnotationApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public MangakaAnnotationApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IReadOnlyList<ChapterPageAnnotationDto>> GetByPageAsync(Guid actorUserId, Guid chapterPageId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/annotations/by-page/{chapterPageId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ChapterPageAnnotationDto>>(content, _jsonOptions) ?? new List<ChapterPageAnnotationDto>();
        }

        public async Task<ChapterPageAnnotationDto> CreateAsync(Guid actorUserId, CreateMangakaAnnotationRequest request, CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/annotations");
            httpRequest.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageAnnotationDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("The annotation was created but no confirmation was returned.");
        }

        public async Task<bool> ResolveAsync(Guid actorUserId, Guid annotationId, string? resolutionNote = null, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/annotations/{annotationId}/resolve");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new ResolveAnnotationRequest(resolutionNote), _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return true;
        }

        private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode) return;

            string message = "The request could not be completed. Please try again.";
            try
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(body)) message = body.Trim('"');
            }
            catch { /* keep default */ }

            throw new InvalidOperationException(message);
        }
    }
}
