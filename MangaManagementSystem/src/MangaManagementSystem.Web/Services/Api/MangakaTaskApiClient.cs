using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.ReadModels;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class MangakaTaskApiClient : BaseApiClient, IMangakaTaskApiClient
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

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

        public async Task<IReadOnlyList<ChapterPageTaskDto>> GetTasksForReviewAsync(Guid actorUserId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/mangaka/tasks");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ChapterPageTaskDto>>(content, _jsonOptions) ?? new List<ChapterPageTaskDto>();
        }

        public async Task<ChapterPageTaskDto?> GetTaskDetailAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/tasks/{taskId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageTaskDto>(content, _jsonOptions);
        }

        public async Task ApproveTaskAsync(Guid actorUserId, Guid taskId, string? reason = null, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/tasks/{taskId}/approve");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { reason }, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }
        }

        public async Task ReturnTaskForReworkAsync(Guid actorUserId, Guid taskId, string reason, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/tasks/{taskId}/return-for-rework");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { reason }, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }
        }

        public async Task CancelTaskAsync(Guid actorUserId, Guid taskId, string reason, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/tasks/{taskId}/cancel");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { reason }, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }
        }

        public async Task<ChapterPageTaskDto> CreateTaskAsync(Guid actorUserId, CreateMangakaTaskRequest request, CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/tasks");
            httpRequest.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChapterPageTaskDto>(content, _jsonOptions)
                ?? throw new InvalidOperationException(
                    "The task was created but no confirmation was returned. Please refresh and verify.");
        }

        public async Task<IReadOnlyList<ChapterPageTaskDto>> GetTasksByPageAsync(Guid actorUserId, Guid chapterPageId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/tasks/by-page/{chapterPageId}");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<ChapterPageTaskDto>>(content, _jsonOptions) ?? new List<ChapterPageTaskDto>();
        }

        public async Task<IReadOnlyList<EligibleAssistantDto>> GetEligibleAssistantsAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/tasks/{taskId}/eligible-assistants");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<EligibleAssistantDto>>(content, _jsonOptions) ?? new List<EligibleAssistantDto>();
        }

        public async Task<ReassignChapterPageTaskResult> ReassignTaskAsync(Guid actorUserId, Guid taskId, ReassignChapterPageTaskRequest reassignRequest, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/mangaka/tasks/{taskId}/reassign");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    newAssignedToUserId = reassignRequest.NewAssignedToUserId,
                    reason = reassignRequest.Reason,
                    updatedTaskDescription = reassignRequest.UpdatedTaskDescription
                }, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ReassignChapterPageTaskResult>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to parse reassignment result.");
        }



        // --- Quick Select ---

        public async Task<IReadOnlyList<QuickSelectChapterDto>> GetQuickSelectChaptersAsync(Guid actorUserId, Guid seriesId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/series/{seriesId}/chapters/quick-select");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<QuickSelectChapterDto>>(content, _jsonOptions) ?? new List<QuickSelectChapterDto>();
        }

        public async Task<IReadOnlyList<QuickSelectPageDto>> GetQuickSelectPagesAsync(Guid chapterId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/chapters/{chapterId}/pages/quick-select");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<QuickSelectPageDto>>(content, _jsonOptions) ?? new List<QuickSelectPageDto>();
        }

        public async Task<IReadOnlyList<QuickSelectAssistantDto>> GetQuickSelectAssistantsAsync(Guid actorUserId, Guid seriesId, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/mangaka/series/{seriesId}/assistants/quick-select");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<QuickSelectAssistantDto>>(content, _jsonOptions) ?? new List<QuickSelectAssistantDto>();
        }

        public async Task<QuickSelectTaskAssignmentResult> QuickSelectAssignAsync(Guid actorUserId, QuickSelectTaskAssignmentRequest quickSelectRequest, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/tasks/quick-select");
            request.Headers.Add(ActorUserIdHeader, actorUserId.ToString());
            request.Content = new StringContent(
                JsonSerializer.Serialize(quickSelectRequest, _jsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ExtractErrorMessageAsync(response, "The request could not be completed.", cancellationToken);
                throw new InvalidOperationException(errorContent);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<QuickSelectTaskAssignmentResult>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to parse quick select assignment result.");
        }
    }
}
