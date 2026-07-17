using System;

namespace MangaManagementSystem.Domain.Policies
{
    public static class SeriesProductionPolicy
    {
        private const string SerializedStatusCode = "SERIALIZED";
        private const string HiatusStatusCode = "HIATUS";

        public static bool AllowsNormalProduction(string? seriesStatusCode)
        {
            return string.Equals(
                       seriesStatusCode,
                       SerializedStatusCode,
                       StringComparison.OrdinalIgnoreCase)
                   || string.Equals(
                       seriesStatusCode,
                       HiatusStatusCode,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
