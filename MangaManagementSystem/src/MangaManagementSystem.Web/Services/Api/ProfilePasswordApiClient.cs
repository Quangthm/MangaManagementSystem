using System.Net.Http.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class ProfilePasswordApiClient
        : BaseApiClient, IProfilePasswordApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProfilePasswordApiClient>
            _logger;

        public ProfilePasswordApiClient(
            HttpClient httpClient,
            ILogger<ProfilePasswordApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendOtpAsync(
            Guid userId)
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/profile/password/otp",
                    new
                    {
                        UserId = userId
                    });

            await EnsureSuccessAsync(
                response,
                "Password OTP");
        }

        public async Task ResetPasswordAsync(
            Guid userId,
            string otpCode,
            string newPassword)
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/profile/password/reset",
                    new
                    {
                        UserId = userId,
                        OtpCode = otpCode,
                        NewPassword = newPassword
                    });

            await EnsureSuccessAsync(
                response,
                "Password reset");
        }

        private async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            string operation)
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

<<<<<<< HEAD
            throw new InvalidOperationException(message);
        }


=======
            throw await ApiResponseReader
                .CreateExceptionAsync(
                    response);
        }
>>>>>>> main
    }
}
