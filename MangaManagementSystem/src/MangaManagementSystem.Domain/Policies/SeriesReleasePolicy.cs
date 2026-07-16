using System;

namespace MangaManagementSystem.Domain.Policies
{
    public static class SeriesReleasePolicy
    {
        public const string SerializedStatusCode = "SERIALIZED";

        public static bool AllowsChapterRelease(string? seriesStatusCode)
        {
            return string.Equals(
                seriesStatusCode,
                SerializedStatusCode,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
