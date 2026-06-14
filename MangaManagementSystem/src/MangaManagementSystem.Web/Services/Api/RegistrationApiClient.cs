using System.Net.Http.Json;
using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Web.Services.Api
{
    public class RegistrationApiClient : IRegistrationApiClient
    {
        private readonly HttpClient _httpClient;

        public RegistrationApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
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

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                throw new InvalidOperationException(error?.Message ?? "This account could not be created.");
            }

            throw new InvalidOperationException("Unable to send verification code. Please try again.");
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

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                throw new InvalidOperationException(error?.Message ?? "The verification code is invalid or has expired.");
            }

            throw new InvalidOperationException("Unable to complete registration. Please try again.");
        }

        private record ApiErrorResponse(string Message);
    }
}
