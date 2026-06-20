using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public class AssistantTaskApiClient : IAssistantTaskApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AssistantTaskApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        // === Read Operations ===

        public async Task<IReadOnlyList<ChapterPageTaskDto>> GetAssignedTasksAsync(Guid actorUserId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/assistant/tasks");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode} ({response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<IReadOnlyList<ChapterPageTaskDto>>(responseContent, _jsonOptions);

            return result ?? new List<ChapterPageTaskDto>();
        }

        public async Task<ChapterPageTaskDto?> GetTaskDetailAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken = default)
        {
            if (taskId == Guid.Empty)
            {
                throw new ArgumentException("Invalid task ID.", nameof(taskId));
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/assistant/tasks/{taskId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode} ({response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ChapterPageTaskDto>(responseContent, _jsonOptions);

            return result;
        }

        // === Annotations ===

        public async Task<IReadOnlyList<ChapterPageAnnotationDto>> GetTaskAnnotationsAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/assistant/tasks/{taskId}/annotations");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Non-critical: return empty list on error so the page still works
                return new List<ChapterPageAnnotationDto>();
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<IReadOnlyList<ChapterPageAnnotationDto>>(responseContent, _jsonOptions);

            return result ?? new List<ChapterPageAnnotationDto>();
        }

        // === Submit Operation ===

        public async Task<AssistantTaskSubmitResultDto?> SubmitTaskWorkAsync(
            Guid actorUserId,
            Guid taskId,
            IBrowserFile file,
            string? versionNote = null,
            CancellationToken cancellationToken = default)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (taskId == Guid.Empty)
            {
                throw new ArgumentException("Invalid task ID.", nameof(taskId));
            }

            // Build multipart form data
            using var content = new MultipartFormDataContent();
            
            // Add file -- use async CopyToAsync to avoid "Synchronous reads are not supported" on Blazor Server streams
            using var ms = new MemoryStream();
            await using (var stream = file.OpenReadStream(10 * 1024 * 1024))
            {
                await stream.CopyToAsync(ms, cancellationToken);
            }
            var fileContent = new ByteArrayContent(ms.ToArray());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            // Add versionNote if provided
            if (!string.IsNullOrWhiteSpace(versionNote))
            {
                content.Add(new StringContent(versionNote, Encoding.UTF8, "text/plain"), "versionNote");
            }

            // Build request with actor header
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/assistant/tasks/{taskId}/submit-work");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = content;

            // Post to API endpoint
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode} ({response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            // Deserialize response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<AssistantTaskSubmitResultDto>(responseContent, _jsonOptions);

            return result;
        }
    }
}
