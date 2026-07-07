using MangaManagementSystem.Application.DTOs.Admin;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class AdminAuditApiClient
        : IAdminAuditApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AdminAuditApiClient>
            _logger;

        public AdminAuditApiClient(
            HttpClient httpClient,
            ILogger<AdminAuditApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
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