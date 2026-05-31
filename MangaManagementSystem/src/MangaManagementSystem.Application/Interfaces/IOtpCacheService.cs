using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IOtpCacheService
    {
        void StoreRegistrationOtp(string email, string otp, RegisterDto request);
        RegisterDto? TryValidateAndRemoveRegistrationOtp(string email, string otp);
    }
}
