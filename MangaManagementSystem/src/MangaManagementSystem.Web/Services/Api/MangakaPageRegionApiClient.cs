using MangaManagementSystem.Application.DTOs.Manga;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class MangakaPageRegionApiClient : BaseApiClient, IMangakaPageRegionApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public MangakaPageRegionApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<PageRegionDto> CreateAsync(Guid actorUserId, CreatePageRegionDto dto, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/regions");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(JsonSerializer.Serialize(dto, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<PageRegionDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("The region was created but no confirmation was returned.");
        }

        public async Task<PageRegionDto> EnsureFullPageRegionAsync(Guid actorUserId, Guid versionId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/regions/version/{versionId}/ensure-full-page");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<PageRegionDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("The full-page region was created but no confirmation was returned.");
        }

        public async Task BulkReplaceAsync(Guid actorUserId, Guid versionId, IReadOnlyList<CreatePageRegionDto> regions, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/mangaka/regions/version/{versionId}/bulk-replace");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new BulkReplaceRegionsRequest(regions), _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }
        }

        public async Task<IReadOnlyList<PageRegionDto>> GetByVersionsAsync(Guid actorUserId, IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/regions/by-versions");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new VersionIdsRequest(versionIds), _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<PageRegionDto>>(content, _jsonOptions) ?? new List<PageRegionDto>();
        }

        public async Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(Guid actorUserId, IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/regions/counts");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new VersionIdsRequest(versionIds), _jsonOptions),
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


    }
}
