using System;

namespace MangaManagementSystem.Domain.Policies
{
    public static class SeriesLifecycleTransitionPolicy
    {
        private const string SerializedStatusCode = "SERIALIZED";
        private const string HiatusStatusCode = "HIATUS";

        public static bool CanSetHiatus(string? currentStatusCode)
        {
            return string.Equals(
                currentStatusCode,
                SerializedStatusCode,
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanResumeSerialization(string? currentStatusCode)
        {
            return string.Equals(
                currentStatusCode,
                HiatusStatusCode,
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanCompleteSeries(string? currentStatusCode)
        {
            return CanSetHiatus(currentStatusCode)
                || CanResumeSerialization(currentStatusCode);
        }
    }
}
