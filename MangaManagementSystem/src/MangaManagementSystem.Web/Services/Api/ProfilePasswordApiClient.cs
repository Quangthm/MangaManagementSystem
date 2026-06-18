using System.Net.Http.Json;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class ProfilePasswordApiClient
        : IProfilePasswordApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProfilePasswordApiClient> _logger;

        public ProfilePasswordApiClient(
            HttpClient httpClient,
            ILogger<ProfilePasswordApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendOtpAsync(Guid userId)
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/profile/password/otp",
                    new
                    {
                        UserId = userId
                    });

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var message =
                await ExtractErrorMessageAsync(
                    response,
                    "Unable to send OTP.");

            _logger.LogWarning(
                "Password OTP API failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task ResetPasswordAsync(
            Guid userId,
            string otpCode,
            string newPassword)
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/profile/password/reset",
                    new
                    {
                        UserId = userId,
                        OtpCode = otpCode,
                        NewPassword = newPassword
                    });

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var message =
                await ExtractErrorMessageAsync(
                    response,
                    "Unable to reset password.");

            _logger.LogWarning(
                "Password reset API failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        private static async Task<string>
            ExtractErrorMessageAsync(
                HttpResponseMessage response,
                string defaultMessage)
        {
            try
            {
                var body =
                    await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    return defaultMessage;
                }

                using var document =
                    JsonDocument.Parse(body);

                var root = document.RootElement;

                if (root.TryGetProperty(
                        "message",
                        out var messageProperty) &&
                    messageProperty.ValueKind ==
                        JsonValueKind.String)
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
                        out var detailProperty) &&
                    detailProperty.ValueKind ==
                        JsonValueKind.String)
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
                        out var titleProperty) &&
                    titleProperty.ValueKind ==
                        JsonValueKind.String)
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
                // Use the safe fallback message below.
            }

            return defaultMessage;
        }
    }
}