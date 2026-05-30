using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterPageVersion : BaseEntity
    {
        public long ChapterPageVersionId { get; set; }
        public long ChapterPageId { get; set; }
        public ChapterPage? ChapterPage { get; set; }
        public short VersionNo { get; set; }
        public long PageFileId { get; set; }
        public FileResource? PageFile { get; set; }
        public string? VersionNote { get; set; }
        public bool IsCurrentVersion { get; set; }
    }
}
