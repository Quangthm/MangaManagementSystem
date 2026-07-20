using System;

namespace MangaManagementSystem.API.Contracts
{
    public sealed class EditorPutChapterOnHoldRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class EditorReleaseChapterRequest
    {
        public bool ConfirmRelease { get; set; }
    }
}
