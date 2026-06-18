using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IOtpCacheService
    {
        void StoreRegistrationOtp(string email, string otp, RegisterDto request);
        RegisterDto? TryValidateAndRemoveRegistrationOtp(string email, string otp);

        void StoreEmailVerificationOtp(string email, string otp);
        bool TryValidateAndRemoveEmailVerificationOtp(string email, string otp);

        void StoreProfileActionOtp(string email, string actionCode, string otp);
        bool TryValidateAndRemoveProfileActionOtp(string email, string actionCode, string otp);
    }
}