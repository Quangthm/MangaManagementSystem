using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IAuthService
    {
        Task<bool> SendRegistrationOtpAsync(RegisterDto request);

        Task<UserDto> CompleteRegistrationWithOtpAsync(
            string email,
            string otp,
            byte[]? portfolioFileBytes = null,
            string? portfolioFileName = null,
            string? portfolioContentType = null);

        Task<AuthResultDto> LoginAsync(LoginDto request);

        Task<AuthResultDto> GetUserByEmailAsync(string email);

        Task<GoogleSignupCallbackResult> ProcessGoogleSignupCallbackAsync(string email, string? googleDisplayName);

        Task<bool> SendEmailVerificationOtpAsync(string email);

        Task<bool> CompleteEmailVerificationOtpAsync(string email, string otp);
    }
}
