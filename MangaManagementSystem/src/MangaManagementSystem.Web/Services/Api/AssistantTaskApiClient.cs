using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public class AssistantTaskApiClient
        : IAssistantTaskApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AssistantTaskApiClient(
            HttpClient httpClient)
        {
            _httpClient =
                httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));

            _jsonOptions =
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
        }

        public async Task<IReadOnlyList<ChapterPageTaskDto>>
            GetAssignedTasksAsync(
                CancellationToken cancellationToken = default)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "api/assistant/tasks");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode} ({response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            var responseContent =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            var result =
                JsonSerializer.Deserialize<
                    IReadOnlyList<ChapterPageTaskDto>>(
                    responseContent,
                    _jsonOptions);

            return result
                   ?? new List<ChapterPageTaskDto>();
        }

        public async Task<ChapterPageTaskDto?>
            GetTaskDetailAsync(
                Guid taskId,
                CancellationToken cancellationToken = default)
        {
            if (taskId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Invalid task ID.",
                    nameof(taskId));
            }

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/assistant/tasks/{taskId}");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode
                    == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                var errorContent =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode} ({response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            var responseContent =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            return JsonSerializer.Deserialize<
                ChapterPageTaskDto>(
                responseContent,
                _jsonOptions);
        }

        public async Task<
            IReadOnlyList<ChapterPageAnnotationDto>>
            GetTaskAnnotationsAsync(
                Guid taskId,
                CancellationToken cancellationToken = default)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/assistant/tasks/{taskId}/annotations");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<
                    ChapterPageAnnotationDto>();
            }

            var responseContent =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            var result =
                JsonSerializer.Deserialize<
                    IReadOnlyList<
                        ChapterPageAnnotationDto>>(
                    responseContent,
                    _jsonOptions);

            return result
                   ?? new List<
                       ChapterPageAnnotationDto>();
        }

        public async Task<
            AssistantCompletedWorkSummaryDto?>
            GetCompletedWorkAsync(
                CancellationToken cancellationToken = default)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "api/assistant/completed-work");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode} ({response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            var responseContent =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            return JsonSerializer.Deserialize<
                AssistantCompletedWorkSummaryDto>(
                responseContent,
                _jsonOptions);
        }

        public async Task<
            AssistantTaskSubmitResultDto?>
            SubmitTaskWorkAsync(
                Guid taskId,
                IBrowserFile file,
                string? versionNote = null,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);

            if (taskId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Invalid task ID.",
                    nameof(taskId));
            }

            using var memoryStream =
                new MemoryStream();

            await using (
                var stream =
                    file.OpenReadStream(
                        10 * 1024 * 1024))
            {
                await stream.CopyToAsync(
                    memoryStream,
                    cancellationToken);
            }

            return await SubmitTaskWorkAsync(
                taskId,
                memoryStream.ToArray(),
                file.Name,
                file.ContentType,
                versionNote,
                cancellationToken);
        }

        public async Task<
            AssistantTaskSubmitResultDto?>
            SubmitTaskWorkAsync(
                Guid taskId,
                byte[] fileBytes,
                string fileName,
                string contentType,
                string? versionNote = null,
                CancellationToken cancellationToken = default)
        {
            if (fileBytes == null
                || fileBytes.Length == 0)
            {
                throw new ArgumentException(
                    "A file is required for submission.",
                    nameof(fileBytes));
            }

            if (taskId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Invalid task ID.",
                    nameof(taskId));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "submission.png";
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "image/png";
            }

            using var content =
                new MultipartFormDataContent();

            using var fileContent =
                new ByteArrayContent(fileBytes);

            fileContent.Headers.ContentType =
                MediaTypeHeaderValue.Parse(
                    contentType);

            content.Add(
                fileContent,
                "file",
                fileName);

            if (!string.IsNullOrWhiteSpace(
                    versionNote))
            {
                content.Add(
                    new StringContent(
                        versionNote,
                        Encoding.UTF8,
                        "text/plain"),
                    "versionNote");
            }

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/assistant/tasks/{taskId}/submit-work")
                {
                    Content = content
                };

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode} ({response.StatusCode}): {errorContent}",
                    null,
                    response.StatusCode);
            }

            var responseContent =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            return JsonSerializer.Deserialize<
                AssistantTaskSubmitResultDto>(
                responseContent,
                _jsonOptions);
        }
    }
}
