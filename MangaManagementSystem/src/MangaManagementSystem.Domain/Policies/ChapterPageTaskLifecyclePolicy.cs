using System;

namespace MangaManagementSystem.Domain.Policies
{
    public static class ChapterPageTaskLifecyclePolicy
    {
        public const string AssignedStatusCode = "ASSIGNED";
        public const string UnderReviewStatusCode = "UNDER_REVIEW";

        public static bool CanCancel(string? currentStatusCode)
        {
            return string.Equals(
                    currentStatusCode,
                    AssignedStatusCode,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    currentStatusCode,
                    UnderReviewStatusCode,
                    StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsActiveTaskStatus(string? currentStatusCode)
        {
            return string.Equals(
                    currentStatusCode,
                    AssignedStatusCode,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    currentStatusCode,
                    UnderReviewStatusCode,
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
