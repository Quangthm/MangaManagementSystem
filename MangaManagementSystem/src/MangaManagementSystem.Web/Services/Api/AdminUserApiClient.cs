using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Web.Options;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class AdminUserApiClient
        : IAdminUserApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AdminUserApiClient>
            _logger;
        private readonly AuthenticationStateProvider
            _authenticationStateProvider;
        private readonly InternalApiOptions
            _internalApiOptions;

        public AdminUserApiClient(
            HttpClient httpClient,
            ILogger<AdminUserApiClient> logger,
            AuthenticationStateProvider
                authenticationStateProvider,
            IOptions<InternalApiOptions>
                internalApiOptions)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authenticationStateProvider =
                authenticationStateProvider;
            _internalApiOptions =
                internalApiOptions.Value;
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

            var users =
                await ApiResponseReader
                    .ReadRequiredAsync<List<UserDto>>(
                        response,
                        "The Admin User API returned an empty response.",
                        cancellationToken);

            return users;
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

        private async Task<HttpRequestMessage>
            CreateAuthorizedRequestAsync(
                HttpMethod method,
                string requestUri)
        {
            var authenticationState =
                await _authenticationStateProvider
                    .GetAuthenticationStateAsync();

            var principal =
                authenticationState.User;

            if (principal.Identity?.IsAuthenticated
                != true)
            {
                throw new InvalidOperationException(
                    "You must be logged in as an administrator.");
            }

            var actorUserIdValue =
                principal.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            var actorRole =
                principal.FindFirst(
                    ClaimTypes.Role)?.Value
                ?? principal.FindFirst("role")?.Value;

            if (!Guid.TryParse(
                    actorUserIdValue,
                    out var actorUserId))
            {
                throw new InvalidOperationException(
                    "The current administrator id is invalid.");
            }

            if (!string.Equals(
                    actorRole,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Administrator access is required.");
            }

            var request =
                new HttpRequestMessage(
                    method,
                    requestUri);

            request.Headers.TryAddWithoutValidation(
                InternalApiOptions.HeaderName,
                _internalApiOptions.Key);

            request.Headers.TryAddWithoutValidation(
                InternalApiOptions.ActorUserIdHeaderName,
                actorUserId.ToString("D"));

            request.Headers.TryAddWithoutValidation(
                InternalApiOptions.ActorRoleHeaderName,
                actorRole);

            return request;
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
