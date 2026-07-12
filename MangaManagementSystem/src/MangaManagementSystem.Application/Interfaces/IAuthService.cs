using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Application.Interfaces;

public interface IAuthService
{
<<<<<<< HEAD
    Task<bool> SendRegistrationOtpAsync(RegisterDto request);
=======
    public interface IAuthService
    {
        Task<bool> SendRegistrationOtpAsync(
            RegisterDto request);
>>>>>>> main

    Task<UserDto> CompleteRegistrationWithOtpAsync(
        string email,
        string otp,
        byte[]? portfolioFileBytes = null,
        string? portfolioFileName = null,
        string? portfolioContentType = null);

<<<<<<< HEAD
    Task<AuthResultDto> LoginAsync(LoginDto request);

    Task<AuthResultDto> GetUserByEmailAsync(string email);

    Task<GoogleSignupCallbackResult> ProcessGoogleSignupCallbackAsync(string email, string? googleDisplayName);

    Task<bool> SendEmailVerificationOtpAsync(string email);

    Task<bool> CompleteEmailVerificationOtpAsync(string email, string otp);
=======
        Task<AuthResultDto> LoginAsync(
            LoginDto request);

        Task<AuthResultDto> GetUserByEmailAsync(
            string email);

        Task<GoogleSignupCallbackResult>
            ProcessGoogleSignupCallbackAsync(
                string email,
                string? googleDisplayName,
                string roleName);

        Task RequestPasswordResetAsync(
            string email,
            string resetPageUrl,
            CancellationToken cancellationToken = default);

        Task ResetPasswordWithTokenAsync(
            string token,
            string newPassword,
            CancellationToken cancellationToken = default);
    }
>>>>>>> main
}
