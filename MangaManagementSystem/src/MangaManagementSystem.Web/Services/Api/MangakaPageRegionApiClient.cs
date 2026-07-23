using MangaManagementSystem.Application.DTOs.Manga;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public class MangakaPageRegionApiClient : IMangakaPageRegionApiClient
    {
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

        public async Task<PageRegionDto> CreateAsync(CreatePageRegionDto dto, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/regions");
            request.Content = new StringContent(JsonSerializer.Serialize(dto, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<PageRegionDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("The region was created but no confirmation was returned.");
        }

        public async Task<PageRegionDto> EnsureFullPageRegionAsync(Guid versionId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/regions/version/{versionId}/ensure-full-page");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<PageRegionDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException("The full-page region was created but no confirmation was returned.");
        }

        public async Task BulkReplaceAsync(Guid versionId, IReadOnlyList<CreatePageRegionDto> regions, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/mangaka/regions/version/{versionId}/bulk-replace");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new BulkReplaceRegionsRequest(regions), _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task<IReadOnlyList<PageRegionDto>> GetByVersionsAsync(IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/regions/by-versions");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new VersionIdsRequest(versionIds), _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<PageRegionDto>>(content, _jsonOptions) ?? new List<PageRegionDto>();
        }

        public async Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/regions/counts");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new VersionIdsRequest(versionIds), _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<Guid, int>>(content, _jsonOptions) ?? new Dictionary<Guid, int>();
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
