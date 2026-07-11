using System.Net.Http.Json;
using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Auth;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class RegistrationApiClient : BaseApiClient, IRegistrationApiClient
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

        public async Task<UserDto> CompleteRegistrationAsync(
            string email,
            string otp,
            byte[]? portfolioFileBytes = null,
            string? portfolioFileName = null,
            string? portfolioContentType = null)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(email), "email");
            form.Add(new StringContent(otp), "otp");

            if (portfolioFileBytes is not null)
            {
                var fileContent = new ByteArrayContent(portfolioFileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    portfolioContentType ?? "application/octet-stream");
                form.Add(fileContent, "portfolioFile", portfolioFileName ?? "portfolio");
            }

            var response = await _httpClient.PostAsync("api/registration/complete", form);

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


    }
}
