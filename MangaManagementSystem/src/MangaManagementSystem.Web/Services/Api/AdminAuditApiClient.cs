using System.Security.Claims;
using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Web.Options;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class AdminAuditApiClient
        : IAdminAuditApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AdminAuditApiClient>
            _logger;
        private readonly AuthenticationStateProvider
            _authenticationStateProvider;
        private readonly InternalApiOptions
            _internalApiOptions;

        public AdminAuditApiClient(
            HttpClient httpClient,
            ILogger<AdminAuditApiClient> logger,
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

        public async Task<AdminAuditPageDto>
            SearchAsync(
                string? search = null,
                string? actionCode = null,
                string? entityType = null,
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
                "actionCode",
                actionCode);

            AddQueryValue(
                query,
                "entityType",
                entityType);

            AddQueryDate(
                query,
                "fromUtc",
                fromUtc);

            AddQueryDate(
                query,
                "toUtc",
                toUtc);

            var route =
                "api/admin/audit-events?"
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
                "Search audit events");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminAuditPageDto>(
                    response,
                    "The Admin Audit API returned an empty search response.",
                    cancellationToken);
        }

        public async Task<AdminAuditFilterOptionsDto>
            GetFilterOptionsAsync(
                CancellationToken cancellationToken = default)
        {
            using var request =
                await CreateAuthorizedRequestAsync(
                    HttpMethod.Get,
                    "api/admin/audit-events/filter-options");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            LogFailure(
                response,
                "Load audit filter options");

            return await ApiResponseReader
                .ReadRequiredAsync<AdminAuditFilterOptionsDto>(
                    response,
                    "The Admin Audit API returned empty filter options.",
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