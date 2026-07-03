using System;

namespace MangaManagementSystem.API.Contracts
{
    public sealed class EditorRescheduleChapterRequest
    {
        public DateTime NewPlannedReleaseDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class EditorPutChapterOnHoldRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
