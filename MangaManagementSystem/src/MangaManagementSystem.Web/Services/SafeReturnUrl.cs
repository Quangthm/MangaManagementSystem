using System;

namespace MangaManagementSystem.Web.Services
{
    public static class SafeReturnUrl
    {
        public static bool IsSafe(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!value.StartsWith("/", StringComparison.Ordinal))
            {
                return false;
            }

            if (value.StartsWith("//", StringComparison.Ordinal))
            {
                return false;
            }

            if (value.Contains("://", StringComparison.Ordinal)
                || value.StartsWith("/\\", StringComparison.Ordinal)
                || value.Contains("\\", StringComparison.Ordinal)
                || value.StartsWith("/javascript:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return value == "/editor"
                || value.StartsWith("/editor/", StringComparison.Ordinal)
                || value.StartsWith("/series/", StringComparison.Ordinal)
                || value == "/editor/series"
                || value == "/editor/proposals"
                || value == "/editor/annotations"
                || value == "/editor/chapters";
        }

        public static string Resolve(string? returnUrl, string fallback)
        {
            return IsSafe(returnUrl) ? returnUrl! : fallback;
        }
    }
}
