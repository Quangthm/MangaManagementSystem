using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace MangaManagementSystem.Infrastructure.Services
{
    public sealed class OtpCacheService : IOtpCacheService
    {
        private const string RegistrationKeyPrefix =
            "registration-otp:";

        private const string ProfileActionKeyPrefix =
            "profile-action-otp:";

        private static readonly TimeSpan OtpTtl =
            TimeSpan.FromMinutes(5);

        private static readonly JsonSerializerOptions
            SerializerOptions =
                new(JsonSerializerDefaults.Web);

        private readonly IDistributedCache
            _distributedCache;

        public OtpCacheService(
            IDistributedCache distributedCache)
        {
            _distributedCache =
                distributedCache
                ?? throw new ArgumentNullException(
                    nameof(distributedCache));
        }

        public void StoreRegistrationOtp(
            string email,
            string otp,
            RegisterDto request)
        {
            var key =
                RegistrationKeyPrefix
                + NormalizeEmail(email);

            var cachedOtp =
                new CachedRegistrationOtp(
                    otp.Trim(),
                    request);

            var serializedValue =
                JsonSerializer.Serialize(
                    cachedOtp,
                    SerializerOptions);

            _distributedCache.SetString(
                key,
                serializedValue,
                CreateCacheEntryOptions());
        }

<<<<<<< HEAD
        public RegisterDto? TryPeekRegistrationOtp(string email)
        {
            var key = RegistrationKeyPrefix + NormalizeEmail(email);
            if (_memoryCache.TryGetValue<CachedRegistrationOtp>(key, out var cached) && cached is not null)
            {
                return cached.Request;
            }
            return null;
        }

        public RegisterDto? TryValidateAndRemoveRegistrationOtp(string email, string otp)
=======
        public RegisterDto?
            TryValidateAndRemoveRegistrationOtp(
                string email,
                string otp)
>>>>>>> main
        {
            var key =
                RegistrationKeyPrefix
                + NormalizeEmail(email);

            var serializedValue =
                _distributedCache.GetString(key);

            if (string.IsNullOrWhiteSpace(
                    serializedValue))
            {
                return null;
            }

            CachedRegistrationOtp? cachedOtp;

            try
            {
                cachedOtp =
                    JsonSerializer.Deserialize<
                        CachedRegistrationOtp>(
                            serializedValue,
                            SerializerOptions);
            }
            catch (JsonException)
            {
                _distributedCache.Remove(key);
                return null;
            }

            if (cachedOtp is null
                || !string.Equals(
                    cachedOtp.Otp,
                    otp?.Trim(),
                    StringComparison.Ordinal))
            {
                return null;
            }

            _distributedCache.Remove(key);

            return cachedOtp.Request;
        }

        public void StoreProfileActionOtp(
            string email,
            string actionCode,
            string otp)
        {
            var key =
                BuildProfileActionKey(
                    email,
                    actionCode);

            _distributedCache.SetString(
                key,
                otp.Trim(),
                CreateCacheEntryOptions());
        }

        public bool TryValidateAndRemoveProfileActionOtp(
            string email,
            string actionCode,
            string otp)
        {
            var key =
                BuildProfileActionKey(
                    email,
                    actionCode);

            var cachedOtp =
                _distributedCache.GetString(key);

            if (string.IsNullOrWhiteSpace(
                    cachedOtp)
                || !string.Equals(
                    cachedOtp,
                    otp?.Trim(),
                    StringComparison.Ordinal))
            {
                return false;
            }

            _distributedCache.Remove(key);

            return true;
        }

        private static DistributedCacheEntryOptions
            CreateCacheEntryOptions()
        {
            return new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    OtpTtl
            };
        }

        private static string BuildProfileActionKey(
            string email,
            string actionCode)
        {
            return ProfileActionKeyPrefix
                + NormalizeEmail(email)
                + ":"
                + NormalizeActionCode(actionCode);
        }

        private static string NormalizeEmail(
            string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Email is required.",
                    nameof(email));
            }

            return email
                .Trim()
                .ToLowerInvariant();
        }

        private static string NormalizeActionCode(
            string actionCode)
        {
            if (string.IsNullOrWhiteSpace(
                    actionCode))
            {
                throw new ArgumentException(
                    "Action code is required.",
                    nameof(actionCode));
            }

            return actionCode
                .Trim()
                .ToUpperInvariant();
        }

        private sealed record CachedRegistrationOtp(
            string Otp,
            RegisterDto Request);
    }
}
