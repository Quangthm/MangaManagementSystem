using System.Net;
using System.Net.Http.Json;
using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class AdminUserApiClient
        : IAdminUserApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AdminUserApiClient>
            _logger;

        public AdminUserApiClient(
            HttpClient httpClient,
            ILogger<AdminUserApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IReadOnlyList<UserDto>>
            GetUsersAsync(
                string? statusCode = null,
                CancellationToken cancellationToken = default)
        {
            var route =
                string.IsNullOrWhiteSpace(statusCode)
                    ? "api/admin/users"
                    : "api/admin/users?status="
                        + Uri.EscapeDataString(
                            statusCode.Trim());

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
                "Load admin users");

            return await ApiResponseReader
                .ReadRequiredAsync<List<UserDto>>(
                    response,
                    "The Admin User API returned an empty response.",
                    cancellationToken);
        }

        public async Task<AdminUserPageDto>
            SearchUsersAsync(
                string? search = null,
                string? statusCode = null,
                string? roleName = null,
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
                "status",
                statusCode);

            AddQueryValue(
                query,
                "role",
                roleName);

            var route =
                "api/admin/users/search?"
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
                "Search admin users");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminUserPageDto>(
                    response,
                    "The Admin User API returned an empty search response.",
                    cancellationToken);
        }

        public async Task<AdminUserDetailDto?>
            GetUserAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Get,
                    $"api/admin/users/{userId:D}");

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
                "Load admin user detail");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminUserDetailDto>(
                    response,
                    "The Admin User API returned an empty user detail response.",
                    cancellationToken);
        }

        public async Task<AdminUserAuditPageDto>
            GetUserAuditAsync(
                Guid userId,
                int pageNumber = 1,
                int pageSize = 20,
                CancellationToken cancellationToken = default)
        {
            var route =
                $"api/admin/users/{userId:D}/audit"
                + $"?pageNumber={pageNumber}"
                + $"&pageSize={pageSize}";

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
                "Load user audit history");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminUserAuditPageDto>(
                    response,
                    "The Admin User API returned an empty audit response.",
                    cancellationToken);
        }

        public async Task<AdminUserStatusCountsDto>
            GetStatusCountsAsync(
                CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Get,
                    "api/admin/users/status-counts");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Load admin user status counts");

            return await ApiResponseReader
                .ReadRequiredAsync<
                    AdminUserStatusCountsDto>(
                    response,
                    "The Admin User API returned empty status counts.",
                    cancellationToken);
        }

        public async Task<FileResourceDto?>
            GetPortfolioAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Get,
                    $"api/admin/users/{userId:D}/portfolio");

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
                "Load user portfolio");

            return await ApiResponseReader
                .ReadRequiredAsync<FileResourceDto>(
                    response,
                    "The Admin User API returned an empty portfolio response.",
                    cancellationToken);
        }

        public Task<UserDto> ApproveUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return SendActionAsync(
                userId,
                "approve",
                null,
                cancellationToken);
        }

        public Task<UserDto> RejectUserAsync(
            Guid userId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            return SendActionAsync(
                userId,
                "reject",
                reason,
                cancellationToken);
        }

        public Task<UserDto> DisableUserAsync(
            Guid userId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            return SendActionAsync(
                userId,
                "disable",
                reason,
                cancellationToken);
        }

        public Task<UserDto> ActivateUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return SendActionAsync(
                userId,
                "activate",
                null,
                cancellationToken);
        }

        public async Task SendPasswordResetAsync(
            Guid userId,
            string resetPageUrl,
            CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Post,
                    $"api/admin/users/{userId:D}/password-reset");

            request.Content =
                JsonContent.Create(
                    new
                    {
                        ResetPageUrl =
                            resetPageUrl
                    });

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Send Admin password reset");

            if (!response.IsSuccessStatusCode)
            {
                throw await ApiResponseReader
                    .CreateExceptionAsync(
                        response,
                        cancellationToken);
            }
        }

        private async Task<UserDto> SendActionAsync(
            Guid userId,
            string action,
            string? reason,
            CancellationToken cancellationToken)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Post,
                    $"api/admin/users/{userId:D}/{action}");

            request.Content =
                JsonContent.Create(
                    new
                    {
                        Reason =
                            string.IsNullOrWhiteSpace(reason)
                                ? null
                                : reason.Trim()
                    });

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                $"Admin user action {action}");

            return await ApiResponseReader
                .ReadRequiredAsync<UserDto>(
                    response,
                    "The Admin User API returned an empty action response.",
                    cancellationToken);
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
