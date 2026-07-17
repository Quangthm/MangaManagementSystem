using System;

namespace MangaManagementSystem.Domain.Policies
{
    public static class ChapterPageTaskCreationPolicy
    {
        private const string DraftStatusCode = "DRAFT";
        private const string RevisionRequestedStatusCode = "REVISION_REQUESTED";

        public static bool CanCreateTask(string? chapterStatusCode)
        {
            return string.Equals(
                       chapterStatusCode,
                       DraftStatusCode,
                       StringComparison.OrdinalIgnoreCase)
                   || string.Equals(
                       chapterStatusCode,
                       RevisionRequestedStatusCode,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
