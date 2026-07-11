using MangaManagementSystem.Application.DTOs.Manga;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class MangakaPageApiClient : BaseApiClient, IMangakaPageApiClient
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
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ChapterPageDto>>(content, _jsonOptions) ?? new List<ChapterPageDto>();
        }

        public async Task<ChapterPageDto?> GetByIdAsync(Guid actorUserId, Guid pageId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/pages/{pageId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

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
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

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
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageDto>(content, _jsonOptions);
        }

        public async Task<bool> DeleteAsync(Guid actorUserId, Guid pageId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/mangaka/pages/{pageId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }
            return true;
        }

        public async Task<CreatePageWithVersionResponseDto?> CreatePageWithVersionAsync(Guid actorUserId, CreatePageWithVersionRequestDto requestBody, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/pages/create-with-file");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CreatePageWithVersionResponseDto>(content, _jsonOptions);
        }

        public async Task<IReadOnlyList<ChapterPageVersionDto>> GetVersionsByPageIdsAsync(Guid actorUserId, IReadOnlyList<Guid> pageIds, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/pages/versions/by-page-ids");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(JsonSerializer.Serialize(new GetVersionsByPageIdsRequest(pageIds), _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ChapterPageVersionDto>>(content, _jsonOptions) ?? new List<ChapterPageVersionDto>();
        }

        public async Task<ChapterPageVersionDto?> GetVersionByIdAsync(Guid actorUserId, Guid versionId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/pages/versions/{versionId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageVersionDto>(content, _jsonOptions);
        }

        public async Task<ChapterPageVersionDto?> CreateVersionWithFileAndRegionsAsync(Guid actorUserId, CreateVersionWithFileAndRegionsRequestDto requestBody, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/pages/versions/create-with-file");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageVersionDto>(content, _jsonOptions);
        }

        public async Task<ChapterPageVersionDto?> UpdateVersionAsync(Guid actorUserId, UpdateChapterPageVersionDto requestBody, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, "api/mangaka/pages/versions");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageVersionDto>(content, _jsonOptions);
        }

        public async Task<bool> SetCurrentVersionAsync(Guid actorUserId, Guid pageId, Guid versionId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/mangaka/pages/{pageId}/versions/set-current");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(JsonSerializer.Serialize(new SetCurrentVersionRequest(versionId), _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }
            return true;
        }

        public async Task<DeleteVersionImageResultDto?> DeleteVersionImageAsync(Guid actorUserId, Guid versionId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/mangaka/pages/versions/{versionId}/image");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DeleteVersionImageResultDto>(content, _jsonOptions);
        }

        public async Task<IReadOnlyList<FileResourceDto>> GetFileResourcesByIdsAsync(Guid actorUserId, IReadOnlyList<Guid> fileIds, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/pages/files/by-ids");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(JsonSerializer.Serialize(new GetFileResourcesByIdsRequest(fileIds), _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<FileResourceDto>>(content, _jsonOptions) ?? new List<FileResourceDto>();
        }


    }
}
