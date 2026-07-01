using MangaManagementSystem.Application.DTOs.Manga;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public class MangakaPageApiClient : IMangakaPageApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public MangakaPageApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IReadOnlyList<ChapterPageDto>> GetByChapterAsync(Guid actorUserId, Guid chapterId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/pages/by-chapter/{chapterId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ChapterPageDto>>(content, _jsonOptions) ?? new List<ChapterPageDto>();
        }

        public async Task<ChapterPageDto?> GetByIdAsync(Guid actorUserId, Guid pageId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/pages/{pageId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageDto>(content, _jsonOptions);
        }

        public async Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(Guid actorUserId, IReadOnlyList<Guid> chapterIds, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/pages/counts");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new PageCountsRequest(chapterIds), _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<Guid, int>>(content, _jsonOptions) ?? new Dictionary<Guid, int>();
        }

        public async Task<ChapterPageDto?> UpdateNotesAsync(Guid actorUserId, Guid pageId, string? pageNotes, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/mangaka/pages/{pageId}/notes");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new UpdatePageNotesRequest(pageNotes), _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageDto>(content, _jsonOptions);
        }

        public async Task<bool> DeleteAsync(Guid actorUserId, Guid pageId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/mangaka/pages/{pageId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

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
            catch { /* keep default message */ }

            throw new InvalidOperationException(message);
        }
    }
}
