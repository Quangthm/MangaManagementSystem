using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IRegistrationApiClient
    {
        Task SendOtpAsync(string username, string email, string password, string roleName, string? displayName);

        Task<UserDto> CompleteRegistrationAsync(
            string email,
            string otp,
            byte[]? portfolioFileBytes = null,
            string? portfolioFileName = null,
            string? portfolioContentType = null);
    }
}
