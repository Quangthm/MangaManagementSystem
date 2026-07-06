using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MangaManagementSystem.Infrastructure.Services
{
    public sealed class PasswordResetTokenService
        : IPasswordResetTokenService
    {
        private const string TokenKeyPrefix =
            "password-reset-token:";

        private const string UserKeyPrefix =
            "password-reset-user:";

        private readonly IMemoryCache _memoryCache;
        private readonly object _gate = new();

        public PasswordResetTokenService(
            IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public string IssueToken(
            Guid userId,
            string email,
            TimeSpan lifetime)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "User id is required.",
                    nameof(userId));
            }

            if (lifetime <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lifetime));
            }

            var normalizedEmail =
                NormalizeEmail(email);

            var token =
                ToBase64Url(
                    RandomNumberGenerator.GetBytes(32));

            var tokenHash =
                HashToken(token);

            var expiresAtUtc =
                DateTimeOffset.UtcNow.Add(lifetime);

            var payload =
                new PasswordResetTokenPayload(
                    userId,
                    normalizedEmail,
                    expiresAtUtc);

            var userKey =
                BuildUserKey(userId);

            lock (_gate)
            {
                if (_memoryCache.TryGetValue<string>(
                        userKey,
                        out var previousTokenHash)
                    && !string.IsNullOrWhiteSpace(
                        previousTokenHash))
                {
                    _memoryCache.Remove(
                        BuildTokenKey(
                            previousTokenHash));
                }

                _memoryCache.Set(
                    BuildTokenKey(tokenHash),
                    payload,
                    expiresAtUtc);

                _memoryCache.Set(
                    userKey,
                    tokenHash,
                    expiresAtUtc);
            }

            return token;
        }

        public bool TryConsumeToken(
            string token,
            out PasswordResetTokenPayload payload)
        {
            payload = default!;

            if (string.IsNullOrWhiteSpace(token)
                || token.Length > 200)
            {
                return false;
            }

            var tokenHash =
                HashToken(token.Trim());

            var tokenKey =
                BuildTokenKey(tokenHash);

            lock (_gate)
            {
                if (!_memoryCache.TryGetValue<
                        PasswordResetTokenPayload>(
                            tokenKey,
                            out var cachedPayload)
                    || cachedPayload is null
                    || cachedPayload.ExpiresAtUtc
                        <= DateTimeOffset.UtcNow)
                {
                    _memoryCache.Remove(tokenKey);
                    return false;
                }

                var userKey =
                    BuildUserKey(
                        cachedPayload.UserId);

                if (!_memoryCache.TryGetValue<string>(
                        userKey,
                        out var activeTokenHash)
                    || !FixedTimeEquals(
                        tokenHash,
                        activeTokenHash))
                {
                    _memoryCache.Remove(tokenKey);
                    return false;
                }

                _memoryCache.Remove(tokenKey);
                _memoryCache.Remove(userKey);

                payload = cachedPayload;
                return true;
            }
        }

        private static string BuildTokenKey(
            string tokenHash) =>
                TokenKeyPrefix + tokenHash;

        private static string BuildUserKey(
            Guid userId) =>
                UserKeyPrefix + userId.ToString("N");

        private static string HashToken(
            string token)
        {
            return Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(token)));
        }

        private static string ToBase64Url(
            byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static bool FixedTimeEquals(
            string left,
            string? right)
        {
            if (string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            var leftBytes =
                Encoding.UTF8.GetBytes(left);

            var rightBytes =
                Encoding.UTF8.GetBytes(right);

            return leftBytes.Length == rightBytes.Length
                && CryptographicOperations.FixedTimeEquals(
                    leftBytes,
                    rightBytes);
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

            return email.Trim().ToLowerInvariant();
        }
    }
}
