using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace MangaManagementSystem.Infrastructure.Services
{
    /// <summary>
    /// In-memory OTP cache adapter. Lives in Infrastructure so any host
    /// (Web or API) can reuse the same implementation through <see cref="IOtpCacheService"/>.
    /// </summary>
    public class OtpCacheService : IOtpCacheService
    {
        private const string RegistrationKeyPrefix = "registration-otp:";
        private const string EmailVerificationKeyPrefix = "email-verification-otp:";
        private const string ProfileActionKeyPrefix = "profile-action-otp:";
        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);

        private readonly IMemoryCache _memoryCache;

        public OtpCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void StoreRegistrationOtp(string email, string otp, RegisterDto request)
        {
            var key = RegistrationKeyPrefix + NormalizeEmail(email);
            _memoryCache.Set(key, new CachedRegistrationOtp(otp, request), OtpTtl);
        }

        public RegisterDto? TryValidateAndRemoveRegistrationOtp(string email, string otp)
        {
            var key = RegistrationKeyPrefix + NormalizeEmail(email);

            if (!_memoryCache.TryGetValue<CachedRegistrationOtp>(key, out var cached) ||
                cached is null ||
                !string.Equals(cached.Otp, otp, StringComparison.Ordinal))
            {
                return null;
            }

            _memoryCache.Remove(key);
            return cached.Request;
        }

        public void StoreEmailVerificationOtp(string email, string otp)
        {
            var key = EmailVerificationKeyPrefix + NormalizeEmail(email);
            _memoryCache.Set(key, otp, OtpTtl);
        }

        public bool TryValidateAndRemoveEmailVerificationOtp(string email, string otp)
        {
            var key = EmailVerificationKeyPrefix + NormalizeEmail(email);

            if (!_memoryCache.TryGetValue<string>(key, out var cachedOtp) ||
                !string.Equals(cachedOtp, otp, StringComparison.Ordinal))
            {
                return false;
            }

            _memoryCache.Remove(key);
            return true;
        }

        public void StoreProfileActionOtp(string email, string actionCode, string otp)
        {
            var key = BuildProfileActionKey(email, actionCode);
            _memoryCache.Set(key, otp, OtpTtl);
        }

        public bool TryValidateAndRemoveProfileActionOtp(string email, string actionCode, string otp)
        {
            var key = BuildProfileActionKey(email, actionCode);

            if (!_memoryCache.TryGetValue<string>(key, out var cachedOtp) ||
                !string.Equals(cachedOtp, otp?.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            _memoryCache.Remove(key);
            return true;
        }

        private static string BuildProfileActionKey(string email, string actionCode)
        {
            return ProfileActionKeyPrefix + NormalizeEmail(email) + ":" + NormalizeActionCode(actionCode);
        }

        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();

        private static string NormalizeActionCode(string actionCode)
            => actionCode.Trim().ToUpperInvariant();

        private sealed record CachedRegistrationOtp(string Otp, RegisterDto Request);
    }
}