using System;
using System.Globalization;
using System.Text;

namespace MangaManagementSystem.Application.Common
{
    /// <summary>
    /// Produces URL-safe slugs from free-text titles and normalizes user-supplied slugs.
    /// Used by the series draft workflow so the database receives a stable URL identity.
    /// The database <c>uq_series_slug</c> constraint remains the final uniqueness protection.
    /// </summary>
    public static class SlugGenerator
    {
        private const int MaxSlugLength = 220;

        /// <summary>
        /// Builds a URL-safe slug from a title. Diacritics are stripped, non-alphanumeric
        /// characters become single hyphens, and the result is lower-cased and trimmed.
        /// </summary>
        public static string FromTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title is required to generate a slug.", nameof(title));
            }

            return Slugify(title);
        }

        /// <summary>
        /// Normalizes a user-supplied slug. Falls back to generating one from the title
        /// when the supplied slug is empty or normalizes to nothing.
        /// </summary>
        public static string Normalize(string? suppliedSlug, string title)
        {
            if (!string.IsNullOrWhiteSpace(suppliedSlug))
            {
                string normalized = Slugify(suppliedSlug);
                if (!string.IsNullOrEmpty(normalized))
                {
                    return normalized;
                }
            }

            return FromTitle(title);
        }

        private static string Slugify(string value)
        {
            string decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(decomposed.Length);
            bool lastWasHyphen = false;

            foreach (char ch in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    // Skip accent marks left over from decomposition.
                    continue;
                }

                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                {
                    builder.Append(ch);
                    lastWasHyphen = false;
                }
                else if (!lastWasHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                    lastWasHyphen = true;
                }
            }

            string slug = builder.ToString().Trim('-');

            if (slug.Length > MaxSlugLength)
            {
                slug = slug.Substring(0, MaxSlugLength).Trim('-');
            }

            return slug;
        }
    }
}
