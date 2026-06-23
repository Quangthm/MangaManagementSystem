using System;

namespace MangaManagementSystem.Web.Services
{
    /// <summary>
    /// Validates and resolves return URLs for cross-role navigation.
    /// Only trusted local application route prefixes are allowed.
    /// </summary>
    public static class SafeReturnUrl
    {
        /// <summary>
        /// Approved local route prefixes. Every allowed returnUrl must start with
        /// one of these prefixes (exact match or prefix + "/").
        /// </summary>
        private static readonly string[] AllowedPrefixes =
        {
            "/mangaka",
            "/assistant",
            "/editor",
            "/board-chief",
            "/board",
            "/admin",
            "/series",
            "/dashboard",
        };

        /// <summary>
        /// Returns true when <paramref name="value"/> is a safe local application path
        /// that starts with an approved route prefix.
        /// </summary>
        public static bool IsSafe(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (!value.StartsWith("/", StringComparison.Ordinal))
                return false;

            // Reject protocol-relative, backslash, scheme, javascript/data URIs, and API paths.
            if (value.StartsWith("//", StringComparison.Ordinal)
                || value.Contains("://", StringComparison.Ordinal)
                || value.StartsWith("/\\", StringComparison.Ordinal)
                || value.Contains("\\", StringComparison.Ordinal)
                || value.StartsWith("/javascript:", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/data:", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/signout", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var prefix in AllowedPrefixes)
            {
                if (string.Equals(value, prefix, StringComparison.Ordinal)
                    || value.StartsWith(prefix + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns <paramref name="returnUrl"/> when safe, otherwise <paramref name="fallback"/>.
        /// </summary>
        public static string Resolve(string? returnUrl, string fallback)
        {
            return IsSafe(returnUrl) ? returnUrl! : fallback;
        }

        /// <summary>
        /// Appends a safe <c>returnUrl</c> query parameter to the given target URL.
        /// The returnUrl value is URI-escaped. Returns <paramref name="targetUrl"/>
        /// unchanged when <paramref name="returnUrl"/> is null/empty.
        /// </summary>
        public static string AppendReturnUrl(string targetUrl, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
                return targetUrl;

            var separator = targetUrl.Contains('?') ? "&" : "?";
            return $"{targetUrl}{separator}returnUrl={Uri.EscapeDataString(returnUrl)}";
        }
    }
}
