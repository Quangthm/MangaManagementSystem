using System.Net.Http.Json;
using MangaManagementSystem.Web.Options;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class PasswordResetApiClient
        : IPasswordResetApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly InternalApiOptions
            _internalApiOptions;
        private readonly ILogger<PasswordResetApiClient>
            _logger;

        public PasswordResetApiClient(
            HttpClient httpClient,
            IOptions<InternalApiOptions> internalApiOptions,
            ILogger<PasswordResetApiClient> logger)
        {
            _httpClient = httpClient;
            _internalApiOptions =
                internalApiOptions.Value;
            _logger = logger;
        }

        public async Task RequestPasswordResetAsync(
            string email,
            string resetPageUrl,
            CancellationToken cancellationToken = default)
        {
            using var request =
                CreateInternalRequest(
                    "api/password-reset/request",
                    new
                    {
                        Email = email,
                        ResetPageUrl = resetPageUrl
                    });

            using var response =
                await _httpClient.SendAsync(
                    request,
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
            using var request =
                CreateInternalRequest(
                    "api/password-reset/complete",
                    new
                    {
                        Token = token,
                        NewPassword = newPassword
                    });

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            await EnsureSuccessAsync(
                response,
                "Password reset completion",
                cancellationToken);
        }

        private HttpRequestMessage CreateInternalRequest(
            string requestUri,
            object payload)
        {
            var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUri);

            request.Headers.TryAddWithoutValidation(
                InternalApiOptions.HeaderName,
                _internalApiOptions.Key);

            request.Content =
                JsonContent.Create(payload);

            return request;
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
