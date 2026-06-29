using System.Net.Http.Json;
using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class AuthApiClient
        : IAuthApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthApiClient> _logger;

        public AuthApiClient(
            HttpClient httpClient,
            ILogger<AuthApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<LoginApiResult> LoginAsync(
            string usernameOrEmail,
            string password,
            CancellationToken cancellationToken = default)
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    new
                    {
                        UsernameOrEmail =
                            usernameOrEmail,
                        Password = password
                    },
                    cancellationToken);

            LogFailure(
                response,
                "Login");

            return await ApiResponseReader
                .ReadRequiredAsync<LoginApiResult>(
                    response,
                    "The login response was empty.",
                    cancellationToken);
        }

        public async Task<LoginApiResult>
            ResolveGoogleLoginAsync(
                string email,
                CancellationToken cancellationToken = default)
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/google-login/resolve",
                    new
                    {
                        Email = email
                    },
                    cancellationToken);

            LogFailure(
                response,
                "Google login resolution");

            return await ApiResponseReader
                .ReadRequiredAsync<LoginApiResult>(
                    response,
                    "The Google login response was empty.",
                    cancellationToken);
        }

        public async Task<GoogleSignupCallbackResult>
            ProcessGoogleSignupAsync(
                string email,
                string? googleDisplayName,
                string roleName,
                CancellationToken cancellationToken = default)
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/google-signup",
                    new
                    {
                        Email = email,
                        GoogleDisplayName =
                            googleDisplayName,
                        RoleName = roleName
                    },
                    cancellationToken);

            LogFailure(
                response,
                "Google sign-up");

            return await ApiResponseReader
                .ReadRequiredAsync<
                    GoogleSignupCallbackResult>(
                    response,
                    "The Google sign-up response was empty.",
                    cancellationToken);
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
