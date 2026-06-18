using System.Net.Http.Json;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Web.Options;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class AuthApiClient
        : IAuthApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthApiClient> _logger;
        private readonly InternalApiOptions
            _internalApiOptions;

        public AuthApiClient(
            HttpClient httpClient,
            ILogger<AuthApiClient> logger,
            IOptions<InternalApiOptions>
                internalApiOptions)
        {
            _httpClient = httpClient;
            _logger = logger;
            _internalApiOptions =
                internalApiOptions.Value;
        }

        public async Task<UserDto> LoginAsync(
            string usernameOrEmail,
            string password)
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    new
                    {
                        UsernameOrEmail =
                            usernameOrEmail,
                        Password = password
                    });

            LogFailure(
                response,
                "Login");

            return await ApiResponseReader
                .ReadRequiredAsync<UserDto>(
                    response,
                    "The login response was empty.");
        }

        public async Task<UserDto>
            ResolveGoogleLoginAsync(
                string email)
        {
            using var request =
                CreateInternalRequest(
                    HttpMethod.Post,
                    "api/auth/google-login/resolve",
                    new
                    {
                        Email = email
                    });

            using var response =
                await _httpClient.SendAsync(
                    request);

            LogFailure(
                response,
                "Google login resolution");

            return await ApiResponseReader
                .ReadRequiredAsync<UserDto>(
                    response,
                    "The Google login response was empty.");
        }

        public async Task<GoogleSignupCallbackResult>
            ProcessGoogleSignupAsync(
                string email,
                string? googleDisplayName,
                string roleName)
        {
            using var request =
                CreateInternalRequest(
                    HttpMethod.Post,
                    "api/auth/google-signup",
                    new
                    {
                        Email = email,
                        GoogleDisplayName =
                            googleDisplayName,
                        RoleName = roleName
                    });

            using var response =
                await _httpClient.SendAsync(
                    request);

            LogFailure(
                response,
                "Google sign-up");

            return await ApiResponseReader
                .ReadRequiredAsync<
                    GoogleSignupCallbackResult>(
                    response,
                    "The Google sign-up response was empty.");
        }

        private HttpRequestMessage
            CreateInternalRequest(
                HttpMethod method,
                string requestUri,
                object payload)
        {
            var request =
                new HttpRequestMessage(
                    method,
                    requestUri);

            request.Headers.TryAddWithoutValidation(
                InternalApiOptions.HeaderName,
                _internalApiOptions.Key);

            request.Content =
                JsonContent.Create(payload);

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
