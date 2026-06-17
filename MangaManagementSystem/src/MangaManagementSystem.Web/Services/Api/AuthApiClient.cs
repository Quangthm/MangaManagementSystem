using System.Net.Http.Json;
using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Web.Options;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.Web.Services.Api
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthApiClient> _logger;
        private readonly InternalApiOptions _internalApiOptions;

        public AuthApiClient(
            HttpClient httpClient,
            ILogger<AuthApiClient> logger,
            IOptions<InternalApiOptions> internalApiOptions)
        {
            _httpClient = httpClient;
            _logger = logger;
            _internalApiOptions = internalApiOptions.Value;
        }

        public async Task<UserDto> LoginAsync(
            string usernameOrEmail,
            string password)
        {
            var request = new
            {
                UsernameOrEmail = usernameOrEmail,
                Password = password
            };

            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    request);

            if (response.IsSuccessStatusCode)
            {
                var user =
                    await response.Content
                        .ReadFromJsonAsync<UserDto>();

                return user
                    ?? throw new InvalidOperationException(
                        "The login response was empty.");
            }

            var message =
                await ExtractErrorMessageAsync(response);

            _logger.LogWarning(
                "Login failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<GoogleSignupCallbackResult>
            ProcessGoogleSignupAsync(
                string email,
                string? googleDisplayName,
                string roleName)
        {
            var requestBody = new
            {
                Email = email,
                GoogleDisplayName = googleDisplayName,
                RoleName = roleName
            };

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/auth/google-signup");

            request.Headers.TryAddWithoutValidation(
                InternalApiOptions.HeaderName,
                _internalApiOptions.Key);

            request.Content =
                JsonContent.Create(requestBody);

            using var response =
                await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result =
                    await response.Content
                        .ReadFromJsonAsync<
                            GoogleSignupCallbackResult>();

                return result
                    ?? throw new InvalidOperationException(
                        "The Google sign-up response was empty.");
            }

            var message =
                await ExtractErrorMessageAsync(response);

            _logger.LogWarning(
                "Google sign-up failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        private static async Task<string>
            ExtractErrorMessageAsync(
                HttpResponseMessage response)
        {
            try
            {
                var body =
                    await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    return "The request could not be completed.";
                }

                using var document =
                    JsonDocument.Parse(body);

                var root =
                    document.RootElement;

                if (root.TryGetProperty(
                        "message",
                        out var messageProperty)
                    && messageProperty.ValueKind
                        == JsonValueKind.String)
                {
                    var message =
                        messageProperty.GetString();

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }
                }

                if (root.TryGetProperty(
                        "detail",
                        out var detailProperty)
                    && detailProperty.ValueKind
                        == JsonValueKind.String)
                {
                    var detail =
                        detailProperty.GetString();

                    if (!string.IsNullOrWhiteSpace(detail))
                    {
                        return detail;
                    }
                }

                if (root.TryGetProperty(
                        "title",
                        out var titleProperty)
                    && titleProperty.ValueKind
                        == JsonValueKind.String)
                {
                    var title =
                        titleProperty.GetString();

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return title;
                    }
                }
            }
            catch (JsonException)
            {
                // Use the safe fallback below.
            }

            return "The request could not be completed.";
        }
    }
}
