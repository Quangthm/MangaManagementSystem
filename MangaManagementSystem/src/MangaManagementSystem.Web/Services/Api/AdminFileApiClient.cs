using System.Net;
using System.Net.Http.Json;
using MangaManagementSystem.Application.DTOs.Admin;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class AdminFileApiClient
        : IAdminFileApiClient
    {
        private const string PlaceholderHeaderName =
            "X-File-Placeholder";

        private readonly HttpClient _httpClient;
        private readonly ILogger<AdminFileApiClient>
            _logger;

        public AdminFileApiClient(
            HttpClient httpClient,
            ILogger<AdminFileApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AdminFilePageDto>
            SearchAsync(
                string? search = null,
                string? filePurposeCode = null,
                string? deletedState = "ACTIVE",
                DateTime? fromUtc = null,
                DateTime? toUtc = null,
                int pageNumber = 1,
                int pageSize = 20,
                CancellationToken cancellationToken = default)
        {
            var query =
                new List<string>
                {
                    "pageNumber=" + pageNumber,
                    "pageSize=" + pageSize
                };

            AddQueryValue(
                query,
                "search",
                search);

            AddQueryValue(
                query,
                "filePurposeCode",
                filePurposeCode);

            AddQueryValue(
                query,
                "deletedState",
                deletedState);

            AddQueryDate(
                query,
                "fromUtc",
                fromUtc);

            AddQueryDate(
                query,
                "toUtc",
                toUtc);

            var route =
                "api/admin/files?"
                + string.Join("&", query);

            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Get,
                    route);

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Search Admin files");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminFilePageDto>(
                    response,
                    "The Admin File API returned an empty search response.",
                    cancellationToken);
        }

        public async Task<AdminFileDetailDto?>
            GetByIdAsync(
                Guid fileResourceId,
                CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Get,
                    $"api/admin/files/{fileResourceId:D}");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (response.StatusCode ==
                HttpStatusCode.NotFound)
            {
                return null;
            }

            LogFailure(
                response,
                "Load Admin file detail");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminFileDetailDto>(
                    response,
                    "The Admin File API returned an empty detail response.",
                    cancellationToken);
        }

        public async Task<AdminFileDetailDto>
            DeleteAsync(
                Guid fileResourceId,
                string deleteReason,
                CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Post,
                    $"api/admin/files/{fileResourceId:D}/delete");

            request.Content =
                JsonContent.Create(
                    new
                    {
                        deleteReason
                    });

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Delete Admin file");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminFileDetailDto>(
                    response,
                    "The Admin File API returned an empty delete response.",
                    cancellationToken);
        }

        public Task<AdminFileDetailDto>
            SoftDeleteAsync(
                Guid fileResourceId,
                string deleteReason,
                CancellationToken cancellationToken = default)
        {
            return DeleteAsync(
                fileResourceId,
                deleteReason,
                cancellationToken);
        }

        public async Task<AdminFileCleanupResultDto>
            CleanupAsync(
                Guid fileResourceId,
                CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Post,
                    $"api/admin/files/{fileResourceId:D}/cleanup");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Clean up Admin file storage");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminFileCleanupResultDto>(
                    response,
                    "The Admin File API returned an empty cleanup response.",
                    cancellationToken);
        }

        public async Task<AdminFileCleanupBatchResultDto>
            CleanupDeletedAsync(
                int batchSize = 20,
                CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Post,
                    "api/admin/files/cleanup-deleted?batchSize="
                    + batchSize);

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Clean up deleted Admin file storage");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminFileCleanupBatchResultDto>(
                    response,
                    "The Admin File API returned an empty cleanup batch response.",
                    cancellationToken);
        }

        public Task<AdminFileContentResult>
            GetPreviewAsync(
                Guid fileResourceId,
                CancellationToken cancellationToken = default)
        {
            return GetContentAsync(
                fileResourceId,
                "preview",
                cancellationToken);
        }

        public Task<AdminFileContentResult>
            GetDownloadAsync(
                Guid fileResourceId,
                CancellationToken cancellationToken = default)
        {
            return GetContentAsync(
                fileResourceId,
                "download",
                cancellationToken);
        }

        private async Task<AdminFileContentResult>
            GetContentAsync(
                Guid fileResourceId,
                string operation,
                CancellationToken cancellationToken)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Get,
                    $"api/admin/files/{fileResourceId:D}/{operation}");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogFailure(
                    response,
                    $"Load Admin file {operation}");

                throw await ApiResponseReader
                    .CreateExceptionAsync(
                        response,
                        cancellationToken);
            }

            var content =
                await response.Content
                    .ReadAsByteArrayAsync(
                        cancellationToken);

            var contentType =
                response.Content.Headers.ContentType
                    ?.MediaType
                ?? "application/octet-stream";

            var fileName =
                NormalizeFileName(
                    response.Content.Headers
                        .ContentDisposition
                        ?.FileNameStar
                    ?? response.Content.Headers
                        .ContentDisposition
                        ?.FileName)
                ?? $"file-{fileResourceId:D}";

            string? placeholderReason = null;

            if (response.Headers.TryGetValues(
                    PlaceholderHeaderName,
                    out var values))
            {
                placeholderReason =
                    values.FirstOrDefault();
            }

            return new AdminFileContentResult(
                content,
                contentType,
                fileName,
                !string.IsNullOrWhiteSpace(
                    placeholderReason),
                placeholderReason);
        }

        private static Task<HttpRequestMessage>
            CreateAuthorizedRequestAsync(
                HttpMethod method,
                string requestUri)
        {
            return Task.FromResult(
                new HttpRequestMessage(
                    method,
                    requestUri));
        }

        private static string? NormalizeFileName(
            string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return fileName
                .Trim()
                .Trim('"');
        }

        private static void AddQueryValue(
            ICollection<string> query,
            string name,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            query.Add(
                Uri.EscapeDataString(name)
                + "="
                + Uri.EscapeDataString(value.Trim()));
        }

        private static void AddQueryDate(
            ICollection<string> query,
            string name,
            DateTime? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            query.Add(
                Uri.EscapeDataString(name)
                + "="
                + Uri.EscapeDataString(
                    value.Value.ToString("O")));
        }

        private void LogFailure(
            HttpResponseMessage response,
            string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            _logger.LogWarning(
                "{Operation} failed: {StatusCode} {ReasonPhrase}",
                operation,
                (int)response.StatusCode,
                response.ReasonPhrase);
        }
    }
}
