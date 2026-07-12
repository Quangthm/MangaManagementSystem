using System.Net.Http.Json;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class ProfilePasswordApiClient
        : BaseApiClient, IProfilePasswordApiClient
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


    }
}