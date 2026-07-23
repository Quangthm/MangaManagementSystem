using MangaManagementSystem.Application.DTOs.Manga;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public class MangakaTaskApiClient : IMangakaTaskApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public MangakaTaskApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IReadOnlyList<ChapterPageTaskDto>> GetTasksForReviewAsync(CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/mangaka/tasks");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ChapterPageTaskDto>>(content, _jsonOptions) ?? new List<ChapterPageTaskDto>();
        }

        public async Task<ChapterPageTaskDto?> GetTaskDetailAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/tasks/{taskId}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageTaskDto>(content, _jsonOptions);
        }

        public async Task ApproveTaskAsync(Guid taskId, string? reason = null, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/tasks/{taskId}/approve");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { reason }, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task ReturnTaskForReworkAsync(Guid taskId, string reason, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/tasks/{taskId}/return-for-rework");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { reason }, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task CancelTaskAsync(Guid taskId, string reason, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/tasks/{taskId}/cancel");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { reason }, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task<ChapterPageTaskDto> CreateTaskAsync(CreateMangakaTaskRequest request, CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/tasks");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageTaskDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException(
                    "The task was created but no confirmation was returned. Please refresh and verify.");
        }

        public async Task<IReadOnlyList<ChapterPageTaskDto>> GetTasksByPageAsync(Guid chapterPageId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/tasks/by-page/{chapterPageId}");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ChapterPageTaskDto>>(content, _jsonOptions) ?? new List<ChapterPageTaskDto>();
        }

        public async Task<IReadOnlyList<EligibleAssistantDto>> GetEligibleAssistantsAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/tasks/{taskId}/eligible-assistants");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<EligibleAssistantDto>>(content, _jsonOptions) ?? new List<EligibleAssistantDto>();
        }

        public async Task<ReassignChapterPageTaskResult> ReassignTaskAsync(Guid taskId, ReassignChapterPageTaskRequest reassignRequest, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/tasks/{taskId}/reassign");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    newAssignedToUserId = reassignRequest.NewAssignedToUserId,
                    reason = reassignRequest.Reason,
                    updatedTaskDescription = reassignRequest.UpdatedTaskDescription
                }, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ReassignChapterPageTaskResult>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to parse reassignment result.");
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"API returned {(int)response.StatusCode}: {errorContent}",
                    null,
                    response.StatusCode);
            }
        }

        // --- Quick Select ---

        public async Task<IReadOnlyList<QuickSelectChapterDto>> GetQuickSelectChaptersAsync(Guid seriesId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/series/{seriesId}/chapters/quick-select");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<QuickSelectChapterDto>>(content, _jsonOptions) ?? new List<QuickSelectChapterDto>();
        }

        public async Task<IReadOnlyList<QuickSelectPageDto>> GetQuickSelectPagesAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/chapters/{chapterId}/pages/quick-select");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<QuickSelectPageDto>>(content, _jsonOptions) ?? new List<QuickSelectPageDto>();
        }

        public async Task<IReadOnlyList<QuickSelectAssistantDto>> GetQuickSelectAssistantsAsync(Guid seriesId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/series/{seriesId}/assistants/quick-select");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<QuickSelectAssistantDto>>(content, _jsonOptions) ?? new List<QuickSelectAssistantDto>();
        }

        public async Task<QuickSelectTaskAssignmentResult> QuickSelectAssignAsync(QuickSelectTaskAssignmentRequest quickSelectRequest, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/tasks/quick-select");
            request.Content = new StringContent(
                JsonSerializer.Serialize(quickSelectRequest, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<QuickSelectTaskAssignmentResult>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to parse quick select assignment result.");
        }
    }
}
