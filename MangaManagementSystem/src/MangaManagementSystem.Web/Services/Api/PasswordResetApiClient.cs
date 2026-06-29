using System.Net.Http.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class PasswordResetApiClient
        : IPasswordResetApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PasswordResetApiClient>
            _logger;

        public PasswordResetApiClient(
            HttpClient httpClient,
            ILogger<PasswordResetApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task RequestPasswordResetAsync(
            string email,
            string resetPageUrl,
            CancellationToken cancellationToken = default)
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/password-reset/request",
                    new
                    {
                        Email = email,
                        ResetPageUrl = resetPageUrl
                    },
                    cancellationToken);

            await EnsureSuccessAsync(
                response,
                "Password reset request",
                cancellationToken);
        }

        public async Task ResetPasswordAsync(
            string token,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/password-reset/complete",
                    new
                    {
                        Token = token,
                        NewPassword = newPassword
                    },
                    cancellationToken);

            await EnsureSuccessAsync(
                response,
                "Password reset completion",
                cancellationToken);
        }

        private async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            string operation,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            _logger.LogWarning(
                "{Operation} API failed: {StatusCode} {ReasonPhrase}",
                operation,
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw await ApiResponseReader
                .CreateExceptionAsync(
                    response,
                    cancellationToken);
        }
    }
}
