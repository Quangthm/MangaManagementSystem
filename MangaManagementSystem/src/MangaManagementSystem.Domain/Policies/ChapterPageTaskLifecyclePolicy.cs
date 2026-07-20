using System;

namespace MangaManagementSystem.Domain.Policies
{
    public static class ChapterPageTaskLifecyclePolicy
    {
        private const string AssignedStatusCode = "ASSIGNED";
        private const string UnderReviewStatusCode = "UNDER_REVIEW";

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
    }
}
