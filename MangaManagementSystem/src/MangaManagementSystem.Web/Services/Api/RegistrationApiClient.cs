using System.Net.Http.Json;
using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Auth;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public class RegistrationApiClient : IRegistrationApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RegistrationApiClient> _logger;

        public RegistrationApiClient(HttpClient httpClient, ILogger<RegistrationApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendOtpAsync(string username, string email, string password, string roleName, string? displayName)
        {
            var request = new
            {
                Username = username,
                Email = email,
                Password = password,
                RoleName = roleName,
                DisplayName = displayName
            };

            var response = await _httpClient.PostAsJsonAsync("api/registration/otp", request);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Registration OTP send failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<UserDto> CompleteRegistrationAsync(string email, string otp)
        {
            var request = new
            {
                Email = email,
                Otp = otp
            };

            var response = await _httpClient.PostAsJsonAsync("api/registration/complete", request);

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserDto>();
                return user!;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Registration complete failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return "An unexpected error occurred. Please try again.";
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // ApiErrorResponse: { "message": "..." }
                if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                {
                    var msg = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        return msg;
                    }
                }

                // ProblemDetails / ValidationProblemDetails: { "detail": "...", "title": "...", "errors": { ... } }
                if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    var detail = detailProp.GetString();
                    if (!string.IsNullOrWhiteSpace(detail))
                    {
                        return detail;
                    }
                }

                // ValidationProblemDetails errors dictionary — return the first error message
                if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var error in errorsProp.EnumerateObject())
                    {
                        if (error.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var msg in error.Value.EnumerateArray())
                            {
                                if (msg.ValueKind == JsonValueKind.String)
                                {
                                    var errMsg = msg.GetString();
                                    if (!string.IsNullOrWhiteSpace(errMsg))
                                    {
                                        return errMsg;
                                    }
                                }
                            }
                        }
                    }
                }

                // Fall back to title if nothing else found
                if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    var title = titleProp.GetString();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return title;
                    }
                }
            }
            catch (JsonException)
            {
                // Response body is not valid JSON — ignore and fall through.
            }

            return "An unexpected error occurred. Please try again.";
        }
    }
}
