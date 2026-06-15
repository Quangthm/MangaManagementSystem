using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public class AssistantTaskApiClient : IAssistantTaskApiClient
    {
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

        public async Task<AssistantTaskSubmitResultDto?> SubmitTaskWorkAsync(
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
            
            // Add file
            using var stream = file.OpenReadStream(10 * 1024 * 1024);
            using var reader = new System.IO.BinaryReader(stream);
            var fileBytes = reader.ReadBytes((int)stream.Length);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            // Add versionNote if provided
            if (!string.IsNullOrWhiteSpace(versionNote))
            {
                content.Add(new StringContent(versionNote, Encoding.UTF8, "text/plain"), "versionNote");
            }

            // Post to API endpoint
            var response = await _httpClient.PostAsync($"api/assistant/tasks/{taskId}/submit-work", content, cancellationToken);

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
