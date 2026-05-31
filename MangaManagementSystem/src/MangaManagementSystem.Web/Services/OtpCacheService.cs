using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MangaManagementSystem.Web.Services
{
    public class OtpCacheService : IOtpCacheService
    {
        private const string KeyPrefix = "registration-otp:";
        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);

        private readonly IMemoryCache _memoryCache;

        public OtpCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void StoreRegistrationOtp(string email, string otp, RegisterDto request)
        {
            var key = BuildKey(email);
            _memoryCache.Set(key, new CachedRegistrationOtp(otp, request), OtpTtl);
        }

        public RegisterDto? TryValidateAndRemoveRegistrationOtp(string email, string otp)
        {
            var key = BuildKey(email);

            if (!_memoryCache.TryGetValue<CachedRegistrationOtp>(key, out var cached) ||
                cached is null ||
                !string.Equals(cached.Otp, otp, StringComparison.Ordinal))
            {
                return null;
            }

            _memoryCache.Remove(key);
            return cached.Request;
        }

        private static string BuildKey(string email)
            => KeyPrefix + email.Trim().ToLowerInvariant();

        private sealed record CachedRegistrationOtp(string Otp, RegisterDto Request);
    }
}
